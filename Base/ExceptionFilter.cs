
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System.Net;

namespace Zuhid.Base;

public class ExceptionFilter(ILogger<ExceptionFilter> logger) : IExceptionFilter
{
  public void OnException(ExceptionContext context)
  {
    var message = context.Exception.Message + ", " + string.Join(", ", context.Exception.Data.Keys.Cast<object>().Select(k => $"{k}: {context.Exception.Data[k]}"));
    // var message = context.Exception.Message + ", " + string.Join(", ", context.Exception.Data.Values.Cast<object>());

    logger.LogError(context.Exception, message);

    // 1. Log the exception (use your preferred logging framework)
    // _logger.LogError(context.Exception, "An unhandled exception occurred.");

    // 2. Create the 500 response
    context.Result = new ObjectResult(new
    {
      Error = "Internal Server Error",
      Message = "An unexpected error occurred on the server.",
      Timestamp = DateTime.UtcNow
    })
    {
      StatusCode = (int)HttpStatusCode.InternalServerError // 500
    };

    // 3. Mark the exception as handled
    context.ExceptionHandled = true;
  }
}
