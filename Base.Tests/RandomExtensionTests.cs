using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace Zuhid.Base.Tests;

public enum SampleRandomEnum { First, Second, Third }

public class SampleRandomObject
{
    public string StringProp { get; set; } = "";
    public short ShortProp { get; set; }
    public int IntProp { get; set; }
    public long LongProp { get; set; }
    public float FloatProp { get; set; }
    public double DoubleProp { get; set; }
    public decimal DecimalProp { get; set; }
    public bool BoolProp { get; set; }
    public char CharProp { get; set; }
    public DateTime DateTimeProp { get; set; }
    public Guid GuidProp { get; set; }
    public SampleRandomEnum EnumProp { get; set; }
    public byte[]? UnsupportedProp { get; set; } // NextValue returns null for unsupported types
    public int ReadOnlyProp => 42;               // CanWrite == false → skipped by NextObject
}

public class RandomExtensionTests
{
    private static readonly string[] Words =
    [
        "able", "big", "child", "day", "early",
        "find", "give", "hand", "know", "last",
        "make", "next", "other", "place", "right",
        "same", "take", "want", "young",
    ];

    private const string SourceCharset = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~`!@#$%^&*()_-+={[}]|\\:;\"'<,>.?/";

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(50)]
    public void NextString_Should_ReturnStringOfGivenLength(int length)
    {
        var random = new Random(42);

        var result = random.NextString(length);

        Assert.Equal(length, result.Length);
    }

    [Fact]
    public void NextString_Should_ReturnCharactersFromExpectedCharset()
    {
        var random = new Random(42);

        var result = random.NextString(200);

        Assert.All(result, c => Assert.Contains(c, SourceCharset));
    }

    [Fact]
    public void NextString_Should_ReturnEmptyString_When_LengthIsZero()
    {
        var random = new Random(1);

        var result = random.NextString(0);

        Assert.Equal(string.Empty, result);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(200)]
    public void NextWord_Should_ReturnResultNotExceedingRequestedLength(int length)
    {
        var random = new Random(7);

        for (var i = 0; i < 50; i++)
        {
            var word = random.NextWord(length);
            Assert.True(word.Length <= length, $"Expected '{word}' (length {word.Length}) to be <= {length}.");
        }
    }

    [Theory]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(50)]
    [InlineData(200)]
    public void NextWord_Should_ReturnWordsFromFixedArrayJoinedBySingleSpaces(int length)
    {
        var random = new Random(9);

        for (var i = 0; i < 50; i++)
        {
            var word = random.NextWord(length);
            if (word.Length == 0)
            {
                continue;
            }

            var tokens = word.Split(' ');
            Assert.All(tokens, token => Assert.Contains(token, Words));
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void NextWord_Should_ReturnEmptyString_When_LengthIsShorterThanShortestWord(int length)
    {
        var random = new Random(3);

        var word = random.NextWord(length);

        Assert.Equal(string.Empty, word);
    }

    [Fact]
    public void NextWord_Should_UseDefaultLengthOfFifty_When_CalledWithoutArguments()
    {
        var random1 = new Random(123);
        var random2 = new Random(123);

        var wordWithoutLength = random1.NextWord();
        var wordWithLength = random2.NextWord(50);

        Assert.Equal(wordWithoutLength, wordWithLength);
        Assert.True(wordWithoutLength.Length <= 50, $"Expected '{wordWithoutLength}' (length {wordWithoutLength.Length}) to be <= 50.");
    }

    [Fact]
    public void NextWord_Should_AppendMultipleWords_When_LengthIsGenerouslyLarge()
    {
        var random = new Random(42);

        var word = random.NextWord(1000);

        var tokens = word.Split(' ');
        Assert.True(tokens.Length > 1, $"Expected '{word}' to be composed of more than one word.");
        Assert.All(tokens, token => Assert.Contains(token, Words));
        Assert.True(word.Length <= 1000, $"Expected '{word}' (length {word.Length}) to be <= 1000.");
    }

    [Fact]
    public void NextListItem_Should_ReturnElementFromGivenArray()
    {
        var random = new Random(11);
        var items = new[] { "red", "green", "blue" };

        for (var i = 0; i < 50; i++)
        {
            var item = random.NextListItem(items);
            Assert.Contains(item, items);
        }
    }

    [Fact]
    public void NextListItem_Should_ReturnOnlyItem_When_ArrayHasSingleElement()
    {
        var random = new Random(99);
        var items = new[] { "solo" };

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal("solo", random.NextListItem(items));
        }
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    public void NextDecimal_Should_ReturnValueWithinExpectedBoundsAndScale(int beforeDecimal, int afterDecimal)
    {
        var random = new Random(55);

        var result = random.NextDecimal(beforeDecimal, afterDecimal);

        var upperBound = (decimal)Math.Pow(10, beforeDecimal);
        Assert.True(result >= 0, $"Expected {result} to be >= 0.");
        Assert.True(result < upperBound, $"Expected {result} to be < {upperBound}.");
        Assert.Equal(result, Math.Round(result, afterDecimal));
    }

    [Fact]
    public void NextDecimal_Should_ReturnWholeNumber_When_AfterDecimalIsZero()
    {
        var random = new Random(21);

        var result = random.NextDecimal(1, 0);

        Assert.Equal(Math.Truncate(result), result);
    }

    [Fact]
    public void NextDecimal_Should_BeDeterministic_ForSameSeed()
    {
        var random1 = new Random(2024);
        var random2 = new Random(2024);

        var result1 = random1.NextDecimal(3, 2);
        var result2 = random2.NextDecimal(3, 2);

        Assert.Equal(result1, result2);
    }

    [Fact]
    public void NextDateTime_Should_ReturnValueWithinExpectedRange()
    {
        var random = new Random(8);
        var minDate = new DateTime(1900, 1, 1);
        var maxMinutes = (int)(new DateTime(2050, 1, 1) - new DateTime(1950, 1, 1)).TotalMinutes;
        var maxDate = minDate.AddMinutes(maxMinutes);

        for (var i = 0; i < 50; i++)
        {
            var result = random.NextDateTime();
            Assert.True(result >= minDate, $"Expected {result} to be >= {minDate}.");
            Assert.True(result < maxDate, $"Expected {result} to be < {maxDate}.");
        }
    }

    [Fact]
    public void NextDateTime_Should_HaveZeroSecondsAndMilliseconds()
    {
        var random = new Random(8);

        for (var i = 0; i < 20; i++)
        {
            var result = random.NextDateTime();
            Assert.Equal(0, result.Second);
            Assert.Equal(0, result.Millisecond);
        }
    }

    [Fact]
    public void NextDateTimeString_Should_MatchExpectedFormat()
    {
        var random = new Random(17);

        var result = random.NextDateTimeString();

        Assert.Matches(@"^new DateTime\(\d{1,4}, \d{1,2}, \d{1,2}, \d{1,2}, \d{1,2}, \d{1,2}\),$", result);
    }

    [Fact]
    public void NextDateTimeString_Should_MatchDateTimeReturnedBySameSeed()
    {
        var random1 = new Random(555);
        var random2 = new Random(555);

        var expected = random1.NextDateTime();
        var literal = random2.NextDateTimeString();

        var match = Regex.Match(literal, @"^new DateTime\((\d+), (\d+), (\d+), (\d+), (\d+), (\d+)\),$");
        Assert.True(match.Success, $"Literal '{literal}' did not match the expected format.");

        var parsed = new DateTime(
            int.Parse(match.Groups[1].Value),
            int.Parse(match.Groups[2].Value),
            int.Parse(match.Groups[3].Value),
            int.Parse(match.Groups[4].Value),
            int.Parse(match.Groups[5].Value),
            int.Parse(match.Groups[6].Value));

        Assert.Equal(expected, parsed);
    }

    [Fact]
    public void NextObject_Should_PopulateSupportedPropertiesAndSkipTheRest()
    {
        var random = new Random(2026);

        var obj = random.NextObject<SampleRandomObject>();

        Assert.NotNull(obj);
        Assert.NotNull(obj.StringProp);
        Assert.NotEqual(Guid.Empty, obj.GuidProp);
        Assert.Contains(obj.EnumProp, Enum.GetValues<SampleRandomEnum>());
        Assert.NotEqual(default, obj.DateTimeProp);
        Assert.Null(obj.UnsupportedProp);   // unsupported type left untouched (NextValue → null)
        Assert.Equal(42, obj.ReadOnlyProp); // read-only property left untouched
    }

    [Fact]
    public void NextObject_Should_BeDeterministic_ForSameSeed()
    {
        var first = new Random(7).NextObject<SampleRandomObject>();
        var second = new Random(7).NextObject<SampleRandomObject>();

        Assert.Equal(first.IntProp, second.IntProp);
        Assert.Equal(first.EnumProp, second.EnumProp);
        Assert.Equal(first.DecimalProp, second.DecimalProp);
    }

    [Fact]
    public void NextListItem_WithDbContext_Should_ReturnIdOfSeededEntity()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new TestDbContext(options);
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        foreach (var id in ids)
        {
            context.TestEntities.Add(new TestEntity { Id = id });
        }
        context.SaveChanges();

        var random = new Random(1);
        var picked = random.NextListItem<TestEntity>(context);

        Assert.Contains(picked, ids);
    }
}
