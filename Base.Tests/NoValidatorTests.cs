namespace Zuhid.Base.Tests;

public class NoValidatorTests
{
    [Fact]
    public void Validate_Should_AlwaysReturnTrue()
    {
        var validator = new NoValidator<TestModel>();

        Assert.True(validator.Validate(new TestModel()));
    }

    [Fact]
    public void Validate_Should_ReturnTrue_ForNullModel()
    {
        var validator = new NoValidator<TestModel>();

        Assert.True(validator.Validate(null!));
    }
}
