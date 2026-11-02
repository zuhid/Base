namespace Zuhid.Base;

public static class ObjectExtension
{
  public static IEnumerable<string> GetMismatchedProperties<T>(this T expected, T actual, params string[] ignoreProperties)
  {
    var mismatchedProperties = new List<string>();
    foreach (var property in typeof(T).GetProperties())
    {
      if (!property.CanRead || ignoreProperties.Contains(property.Name))
      {
        continue;
      }
      if (!Equals(property.GetValue(expected), property.GetValue(actual)))
      {
        mismatchedProperties.Add(property.Name);
      }
    }
    return mismatchedProperties;
  }
}
