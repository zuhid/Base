namespace Zuhid.Base.Tests;

public class ObjectExtensionTests
{
    private sealed class Sample
    {
        public int A { get; set; }
        public string B { get; set; } = "";
        public Guid C { get; set; }
        public string WriteOnly { set { } } // CanRead == false → skipped
    }

    [Fact]
    public void GetMismatchedProperties_Should_ReturnDifferingProperties()
    {
        var expected = new Sample { A = 1, B = "x", C = Guid.Empty };
        var actual = new Sample { A = 2, B = "x", C = Guid.Empty };

        var result = expected.GetMismatchedProperties(actual);

        Assert.Equal(["A"], result);
    }

    [Fact]
    public void GetMismatchedProperties_Should_ReturnEmpty_When_AllPropertiesEqual()
    {
        var expected = new Sample { A = 1, B = "x", C = Guid.Empty };
        var actual = new Sample { A = 1, B = "x", C = Guid.Empty };

        Assert.Empty(expected.GetMismatchedProperties(actual));
    }

    [Fact]
    public void GetMismatchedProperties_Should_SkipIgnoredProperties()
    {
        var expected = new Sample { A = 1, B = "x" };
        var actual = new Sample { A = 2, B = "y" };

        var result = expected.GetMismatchedProperties(actual, "A", "B");

        Assert.Empty(result);
    }
}
