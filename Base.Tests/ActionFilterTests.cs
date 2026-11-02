using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Zuhid.Base.Tests;

public class ActionFilterTests
{
    private static ActionExecutingContext CreateActionExecutingContext(bool validModelState = true)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        if (!validModelState)
        {
            actionContext.ModelState.AddModelError("Field", "Required");
        }
        return new ActionExecutingContext(actionContext, [], new Dictionary<string, object?>(), new object());
    }

    private static ActionExecutedContext CreateActionExecutedContext(bool validModelState = true)
    {
        var actionContext = new ActionContext(new DefaultHttpContext(), new RouteData(), new ActionDescriptor());
        if (!validModelState)
        {
            actionContext.ModelState.AddModelError("Field", "Required");
        }
        return new ActionExecutedContext(actionContext, [], new object());
    }

    [Fact]
    public void OnActionExecuting_ValidModelState_Should_NotSetResult()
    {
        var loggerMock = new Mock<ILogger<ActionFilter>>();
        var filter = new ActionFilter(loggerMock.Object);
        var context = CreateActionExecutingContext(validModelState: true);

        filter.OnActionExecuting(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuting_InvalidModelState_Should_SetBadRequestResult()
    {
        var loggerMock = new Mock<ILogger<ActionFilter>>();
        var filter = new ActionFilter(loggerMock.Object);
        var context = CreateActionExecutingContext(validModelState: false);

        filter.OnActionExecuting(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void OnActionExecuting_InvalidModelState_Should_LogWarning()
    {
        var loggerMock = new Mock<ILogger<ActionFilter>>();
        var filter = new ActionFilter(loggerMock.Object);
        var context = CreateActionExecutingContext(validModelState: false);

        filter.OnActionExecuting(context);

        loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public void OnActionExecuted_ValidModelState_Should_NotSetResult()
    {
        var loggerMock = new Mock<ILogger<ActionFilter>>();
        var filter = new ActionFilter(loggerMock.Object);
        var context = CreateActionExecutedContext(validModelState: true);

        filter.OnActionExecuted(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public void OnActionExecuted_InvalidModelState_Should_SetBadRequestResult()
    {
        var loggerMock = new Mock<ILogger<ActionFilter>>();
        var filter = new ActionFilter(loggerMock.Object);
        var context = CreateActionExecutedContext(validModelState: false);

        filter.OnActionExecuted(context);

        var result = Assert.IsType<BadRequestObjectResult>(context.Result);
        Assert.NotNull(result.Value);
    }

    [Fact]
    public void OnActionExecuted_InvalidModelState_Should_LogWarning()
    {
        var loggerMock = new Mock<ILogger<ActionFilter>>();
        var filter = new ActionFilter(loggerMock.Object);
        var context = CreateActionExecutedContext(validModelState: false);

        filter.OnActionExecuted(context);

        loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
