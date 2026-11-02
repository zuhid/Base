using System.Net;

namespace Zuhid.Base.Tests;

public class ApiResponseTests
{
    [Fact]
    public void Properties_Should_SetAndGet()
    {
        var model = new SimpleResponse { Value = "test" };
        var response = new ApiResponse<SimpleResponse>
        {
            StatusCode = HttpStatusCode.OK,
            Content = "raw content",
            Model = model
        };

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("raw content", response.Content);
        Assert.Same(model, response.Model);
    }

    [Fact]
    public void DefaultValues_Should_BeDefault()
    {
        var response = new ApiResponse<SimpleResponse>();

        Assert.Equal(default, response.StatusCode);
        Assert.Null(response.Content);
        Assert.Null(response.Model);
    }
}
