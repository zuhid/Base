namespace Zuhid.Base.Tests;

public class StringExtensionTests
{
    [Theory]
    [InlineData("HelloWorld", "helloWorld")]
    [InlineData("A", "a")]
    [InlineData("apple", "apple")]
    public void ToCamelCase_Should_ConvertCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.ToCamelCase());
    }

    [Theory]
    [InlineData("helloWorld", "HelloWorld")]
    [InlineData("a", "A")]
    [InlineData("Apple", "Apple")]
    public void ToTitleCase_Should_ConvertCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.ToTitleCase());
    }

    [Theory]
    [InlineData("Hello@World!", "HelloWorld")]
    [InlineData("123-456", "123456")]
    [InlineData("a b c", "abc")]
    public void RemoveSpecialCharacters_Should_RemoveCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.RemoveSpecialCharacters());
    }

    [Theory]
    [InlineData("HelloWorld", "hello-world")]
    [InlineData("MyTestClass", "my-test-class")]
    [InlineData("Already-Kebab", "already---kebab")]
    public void ToKebabCase_Should_ConvertCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.ToKebabCase());
    }

    [Theory]
    [InlineData("HelloWorld", "hello_world")]
    [InlineData("MyTestClass", "my_test_class")]
    public void ToSnakeCase_Should_ConvertCorrectly(string input, string expected)
    {
        Assert.Equal(expected, input.ToSnakeCase());
    }
}
