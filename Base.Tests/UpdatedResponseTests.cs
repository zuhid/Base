namespace Zuhid.Base.Tests;

public class UpdatedResponseTests
{
    [Fact]
    public void Properties_Should_SetAndGet()
    {
        // Arrange
        var model = new UpdatedResponse();
        var updated = DateTime.UtcNow;

        // Act
        model.Updated = updated;

        // Assert
        Assert.Equal(updated, model.Updated);
    }
}

public enum TestEnum
{
    Value1,
    Value2
}
