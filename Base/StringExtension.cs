using System.Text;
using System.Text.RegularExpressions;

namespace Zuhid.Base;

public static class StringExtension
{
  public static string ToCamelCase(this string str)
  {
    return $"{str[..1].ToLower()}{str[1..]}";
  }

  public static string ToTitleCase(this string str)
  {
    return $"{str[..1].ToUpper()}{str[1..]}";
  }

  public static string RemoveSpecialCharacters(this string str)
  {
    var stringBuilder = new StringBuilder();
    foreach (var c in str)
    {
      if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
      {
        _ = stringBuilder.Append(c);
      }
    }
    return stringBuilder.ToString();
  }

  public static string ToKebabCase(this string str)
  {
    var result = Regex.Replace(str, "([A-Z][a-z]|(?<=[a-z])[^a-z]|(?<=[A-Z])[0-9_])", "-$1").ToLower();
    return result.StartsWith('-') ? result[1..] : result;
  }

  public static string ToSnakeCase(this string str)
  {
    var result = Regex.Replace(str, "([A-Z][a-z]|(?<=[a-z])[^a-z]|(?<=[A-Z])[0-9_])", "_$1").ToLower();
    return result.StartsWith('_') ? result[1..] : result;
  }

}
