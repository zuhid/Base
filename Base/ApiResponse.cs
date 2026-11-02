using System.Net;
namespace Zuhid.Base;

public class ApiResponse<TResponse> where TResponse : class
{
  public HttpStatusCode StatusCode { get; set; }
  public string? Content { get; set; }
  public TResponse? Model { get; set; }
}
