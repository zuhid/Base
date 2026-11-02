using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Zuhid.Base.Query;

public static class QueryableExtension
{
  public static IQueryable<T> Filter<T>(this IQueryable<T> query, List<FilterRule> filters)
  {
    foreach (var filter in filters)
    {
      query = query.Filter(filter.FilterBy, filter.FilterValue, filter.FilterOperator);
    }
    return query;
  }

  public static IQueryable<T> Filter<T>(this IQueryable<T> query, string propertyName, string? value, FieldOperator fieldOperator = FieldOperator.Equal)
  {
    var parameter = Expression.Parameter(typeof(T), "n");
    Expression property = Expression.Property(parameter, GetProperty<T>(propertyName));
    var valueType = Nullable.GetUnderlyingType(property.Type) ?? property.Type;
    // String filters match case-insensitively: both sides are lowered (translates to SQL LOWER()).
    if (valueType == typeof(string) && fieldOperator is not (FieldOperator.Null or FieldOperator.NotNull))
    {
      property = Expression.Call(property, nameof(string.ToLower), Type.EmptyTypes);
      value = value?.ToLower();
    }
    var comparison = fieldOperator switch
    {
      FieldOperator.Equal => Expression.Equal(property, GetConstant(value, valueType, property.Type)),
      FieldOperator.NotEqual => Expression.NotEqual(property, GetConstant(value, valueType, property.Type)),
      FieldOperator.LessThan => Expression.LessThan(property, GetConstant(value, valueType, property.Type)),
      FieldOperator.LessThanOrEqual => Expression.LessThanOrEqual(property, GetConstant(value, valueType, property.Type)),
      FieldOperator.GreaterThan => Expression.GreaterThan(property, GetConstant(value, valueType, property.Type)),
      FieldOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(property, GetConstant(value, valueType, property.Type)),
      FieldOperator.Between => Between(property, value, valueType),
      FieldOperator.Null => Expression.Equal(property, Expression.Constant(null, property.Type)),
      FieldOperator.NotNull => Expression.NotEqual(property, Expression.Constant(null, property.Type)),
      FieldOperator.Contains => CallStringMethod(property, propertyName, nameof(string.Contains), value),
      FieldOperator.StartsWith => CallStringMethod(property, propertyName, nameof(string.StartsWith), value),
      FieldOperator.EndsWith => CallStringMethod(property, propertyName, nameof(string.EndsWith), value),
      _ => throw new ArgumentOutOfRangeException(nameof(fieldOperator), fieldOperator, null),
    };
    return query.Where(Expression.Lambda<Func<T, bool>>(comparison, parameter));
  }

  public static IQueryable<T> Sort<T>(this IQueryable<T> query, List<SortRule> sorts)
  {
    for (var i = 0; i < sorts.Count; i++)
    {
      query = query.Sort(sorts[i].SortBy, sorts[i].Descending, i > 0);
    }
    return query;
  }

  // thenBy: null (the default) auto-detects — appends to an already-ordered query with
  // ThenBy, otherwise starts the ordering with OrderBy; pass a value to override.
  public static IQueryable<T> Sort<T>(this IQueryable<T> query, string propertyName, bool descending = false, bool? thenBy = null)
  {
    var parameter = Expression.Parameter(typeof(T), "n");
    var property = Expression.Property(parameter, GetProperty<T>(propertyName));
    var keySelector = Expression.Lambda(property, parameter);
    var method = (thenBy ?? IsOrdered(query.Expression), descending) switch
    {
      (false, false) => nameof(Queryable.OrderBy),
      (false, true) => nameof(Queryable.OrderByDescending),
      (true, false) => nameof(Queryable.ThenBy),
      (true, true) => nameof(Queryable.ThenByDescending),
    };
    var call = Expression.Call(
        typeof(Queryable),
        method,
        [typeof(T), property.Type],
        query.Expression,
        Expression.Quote(keySelector));
    return query.Provider.CreateQuery<T>(call);
  }

  // A query is ordered when an OrderBy/ThenBy call appears in its method chain; only
  // the source argument is walked, so orderings inside nested lambdas don't count.
  private static bool IsOrdered(Expression expression) =>
      expression is MethodCallExpression call
      && (call.Method.Name is nameof(Queryable.OrderBy) or nameof(Queryable.OrderByDescending) or nameof(Queryable.ThenBy) or nameof(Queryable.ThenByDescending)
          || call.Arguments.Count > 0 && IsOrdered(call.Arguments[0]));

  private static Expression GetConstant(string? value, Type valueType, Type propertyType)
  {
    var converted = valueType == typeof(string) ? value : TypeDescriptor.GetConverter(valueType).ConvertFromInvariantString(value ?? string.Empty);
    Expression constant = Expression.Constant(converted, valueType);
    return valueType == propertyType ? constant : Expression.Convert(constant, propertyType);
  }

  private static Expression Between(Expression property, string? value, Type valueType)
  {
    var parts = (value ?? string.Empty).Split(',', 2, StringSplitOptions.TrimEntries);
    var low = GetConstant(parts[0], valueType, property.Type);
    var high = GetConstant(parts[^1], valueType, property.Type);
    return Expression.AndAlso(Expression.GreaterThanOrEqual(property, low), Expression.LessThanOrEqual(property, high));
  }

  private static MethodCallExpression CallStringMethod(Expression property, string propertyName, string methodName, string? value) =>
      property.Type == typeof(string)
          ? Expression.Call(property, methodName, Type.EmptyTypes, Expression.Constant(value ?? string.Empty))
          : throw new ArgumentException($"Operator '{methodName}' requires a string property; '{propertyName}' is {property.Type.Name}.");

  private static PropertyInfo GetProperty<T>(string propertyName) =>
      typeof(T).GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase)
      ?? throw new ArgumentException($"Property '{propertyName}' does not exist on {typeof(T).Name}.", nameof(propertyName));
}
