using System.ComponentModel.DataAnnotations;

namespace Zuhid.Base.Tests;

public class RequiredGuidAttributeTests
{
    private static ValidationContext CreateContext(string memberName = "Id") =>
        new(new object()) { MemberName = memberName };

    [Fact]
    public void IsValid_NullValue_Should_ReturnValidationError()
    {
        var attribute = new RequiredGuidAttribute();
        var result = attribute.GetValidationResult(null, CreateContext("UserId"));
        Assert.NotNull(result);
        Assert.Contains("UserId", result!.ErrorMessage!);
    }

    [Fact]
    public void IsValid_EmptyGuid_Should_ReturnValidationError()
    {
        var attribute = new RequiredGuidAttribute();
        var result = attribute.GetValidationResult(Guid.Empty, CreateContext("OwnerId"));
        Assert.NotNull(result);
        Assert.Contains("OwnerId", result!.ErrorMessage!);
    }

    [Fact]
    public void IsValid_NonGuidType_Should_ReturnValidationError()
    {
        var attribute = new RequiredGuidAttribute();
        var result = attribute.GetValidationResult("not-a-guid", CreateContext("Id"));
        Assert.NotNull(result);
    }

    [Fact]
    public void IsValid_ValidGuid_Should_ReturnSuccess()
    {
        var attribute = new RequiredGuidAttribute();
        var result = attribute.GetValidationResult(Guid.NewGuid(), CreateContext("Id"));
        Assert.Equal(ValidationResult.Success, result);
    }
}
