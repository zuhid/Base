using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;

namespace Zuhid.Base;

public static class RandomExtension
{
  private static readonly ConcurrentDictionary<Type, Guid[]> ListItemIds = new();

  public static string NextString(this Random random, int length)
  {
    var source = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~`!@#$%^&*()_-+={[}]|\\:;\"'<,>.?/";
    var result = Enumerable.Range(0, length).Select(s => source[random.Next(0, source.Length)]);
    return new string([.. result]);
  }

  public static string NextWord(this Random random, int length = 50)
  {
    var stringArray = new string[] {
            "able", "big", "child", "day", "early",
            "find", "give", "hand", "know", "last",
            "make", "next", "other", "place", "right",
            "same", "take", "want", "young",
        };

    var result = new StringBuilder();
    while (true)
    {
      var word = stringArray[random.Next(stringArray.Length)];
      var separator = result.Length > 0 ? " " : "";
      if (result.Length + separator.Length + word.Length > length)
      {
        break;
      }

      result.Append(separator).Append(word);
    }

    return result.ToString();
  }

  public static T NextListItem<T>(this Random random, T[] array)
  {
    return array[random.Next(array.Length)];
  }

  // Picks a random existing row's Id so tests referencing this entity as a foreign key don't violate the FK constraint.
  // The Ids are cached per entity type after the first call — the referenced tables hold stable seeded reference data,
  // so the database is only queried once per entity type instead of on every call.
  public static Guid NextListItem<TEntity>(this Random random, DbContext context) where TEntity : class, IEntity
  {
    var ids = ListItemIds.GetOrAdd(typeof(TEntity), _ => [.. context.Set<TEntity>().Select(e => e.Id)]);
    return random.NextListItem(ids);
  }

  public static decimal NextDecimal(this Random random, int beforeDecimal, int afterDecimal)
  {
    var maxValue = (int)Math.Pow(10, beforeDecimal + afterDecimal) - 1;
    return (decimal)(random.Next(maxValue / 10, maxValue) / Math.Pow(10, afterDecimal));
  }

  public static DateTime NextDateTime(this Random random)
  {
    var maxMinutes = (int)(new DateTime(2050, 1, 1) - new DateTime(1950, 1, 1)).TotalMinutes;
    return new DateTime(1900, 1, 1).AddMinutes(random.Next(maxMinutes));
  }

  public static string NextDateTimeString(this Random random)
  {
    var dateTime = random.NextDateTime();
    return $"new DateTime({dateTime.Year}, {dateTime.Month}, {dateTime.Day}, {dateTime.Hour}, {dateTime.Minute}, {dateTime.Second}),";
  }

  public static T NextObject<T>(this Random random) where T : new()
  {
    var obj = new T();
    var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
    foreach (var property in properties)
    {
      if (!property.CanWrite)
      {
        continue;
      }

      var value = random.NextValue(property.PropertyType);
      if (value != null)
      {
        property.SetValue(obj, value);
      }
    }

    return obj;
  }

  // Type → value factory, replacing a large switch so NextValue stays low-complexity as more types are added.
  private static readonly Dictionary<Type, Func<Random, object>> ValueFactories = new()
  {
    [typeof(string)] = random => random.NextWord(),
    [typeof(short)] = random => (short)random.Next(short.MinValue, short.MaxValue),
    [typeof(int)] = random => random.Next(),
    [typeof(long)] = random => (long)random.Next(),
    [typeof(float)] = random => (float)random.NextDouble(),
    [typeof(double)] = random => random.NextDouble(),
    [typeof(decimal)] = random => random.NextDecimal(5, 2),
    [typeof(bool)] = random => random.Next(2) == 0,
    [typeof(char)] = random => (char)random.Next('a', 'z' + 1),
    [typeof(DateTime)] = random => random.NextDateTime(),
    [typeof(Guid)] = random => Guid.NewGuid(),
  };

  private static object? NextValue(this Random random, Type type)
  {
    var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

    if (underlyingType.IsEnum)
    {
      var values = Enum.GetValues(underlyingType);
      return values.GetValue(random.Next(values.Length));
    }

    return ValueFactories.TryGetValue(underlyingType, out var factory) ? factory(random) : null;
  }
}

