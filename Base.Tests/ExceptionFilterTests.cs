using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Zuhid.Base.Tests;

public class ExceptionFilterTests
{
    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        return new ExceptionContext(actionContext, []) { Exception = exception };
    }

    [Fact]
    public void OnException_Should_Set500Result_And_MarkHandled()
    {
        var loggerMock = new Mock<ILogger<ExceptionFilter>>();
        var filter = new ExceptionFilter(loggerMock.Object);
        var context = CreateExceptionContext(new InvalidOperationException("Test error"));

        filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(500, result.StatusCode);
    }

    [Fact]
    public void OnException_WithExceptionData_Should_IncludeDataInMessage()
    {
        var loggerMock = new Mock<ILogger<ExceptionFilter>>();
        var filter = new ExceptionFilter(loggerMock.Object);
        var exception = new InvalidOperationException("Error with data");
        exception.Data["key"] = "value";
        var context = CreateExceptionContext(exception);

        filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        Assert.Equal(500, ((ObjectResult)context.Result!).StatusCode);
    }

    [Fact]
    public void OnException_Should_LogError()
    {
        var loggerMock = new Mock<ILogger<ExceptionFilter>>();
        var filter = new ExceptionFilter(loggerMock.Object);
        var exception = new InvalidOperationException("Test error");
        var context = CreateExceptionContext(exception);

        filter.OnException(context);

        loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            exception,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
