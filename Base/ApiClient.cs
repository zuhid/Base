using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Zuhid.Base;

public abstract class ApiClient(HttpClient httpClient, IApiClientOptions apiClientOptions, ILogger logger)
{
  protected readonly HttpClient _httpClient = httpClient;
  protected readonly string _baseUrl = apiClientOptions.BaseUrl;
  protected string _authorization = apiClientOptions.Authorization;
  protected readonly ILogger _logger = logger;

  protected static JsonSerializerOptions s_readOptions = new()
  {
    PropertyNameCaseInsensitive = true
  };
  protected static JsonSerializerOptions s_logOptions = new()
  {
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
  };
  protected List<KeyValuePair<string, string>> _customHeaders = [];

  public abstract Task<bool> Login();

  public async Task<ApiResponse<TResponse>> Get<TRequest, TResponse>(string url, TRequest request)
      where TRequest : class
      where TResponse : class
  {
    return await SendRequest<TRequest, TResponse>(HttpMethod.Get, url, request);
  }

  public async Task<ApiResponse<TResponse>> Post<TRequest, TResponse>(string url, TRequest data)
      where TRequest : class
      where TResponse : class
  {
    return await SendRequest<TRequest, TResponse>(HttpMethod.Post, url, data);
  }

  public async Task Post<TRequest>(string url, TRequest data)
      where TRequest : class
  {
    await SendRequest(HttpMethod.Post, url, data);
  }

  public async Task<ApiResponse<TResponse>> Put<TRequest, TResponse>(string url, TRequest data)
      where TRequest : class
      where TResponse : class
  {
    return await SendRequest<TRequest, TResponse>(HttpMethod.Put, url, data);
  }

  // public async Task<ApiResponse<TResponse>> Delete<TResponse>(string url) where TResponse : class
  // {
  //     return await SendRequest<object, TResponse>(HttpMethod.Delete, url);
  // }

  private HttpRequestMessage BuildMessage<TRequest>(HttpMethod method, string url, TRequest data)
      where TRequest : class
  {
    var message = new HttpRequestMessage(method, $"{_baseUrl}/{url}");
    _customHeaders.ForEach(item =>
    {
      if (message.Headers.Contains(item.Key))
      {
        message.Headers.Remove(item.Key);
      }
      message.Headers.Add(item.Key, item.Value);
    });
    if (data != null)
    {
      message.Content = new StringContent(JsonSerializer.Serialize(data, s_logOptions), System.Text.Encoding.UTF8, "application/json");
    }
    return message;
  }

  private async Task SendRequest<TRequest>(HttpMethod method, string url, TRequest data)
      where TRequest : class
  {
    if (!await Login())
    {
      return;
    }

    var message = BuildMessage(method, url, data);
    try
    {
      var response = await _httpClient.SendAsync(message);
      if (response != null && !response.IsSuccessStatusCode)
      {
        Log(LogLevel.Warning, JsonSerializer.Serialize(new
        {
          Url = message.RequestUri,
          response.StatusCode,
          response.Content
        }));
      }
      Log(LogLevel.Information, new
      {
        Url = $"{_baseUrl}/{url}",
        StatusCode = response?.IsSuccessStatusCode,
        Content = JsonSerializer.Serialize(data, s_logOptions)
      });
    }
    catch (Exception ex)
    {
      ex.Data.Add("Url", $"{_baseUrl}/{url}");
      ex.Data.Add("data", data);
      throw;
    }
  }

  private async Task<ApiResponse<TResponse>> SendRequest<TRequest, TResponse>(HttpMethod method, string url, TRequest data)
      where TRequest : class
      where TResponse : class
  {
    if (!await Login())
    {
      return new ApiResponse<TResponse>();
    }

    var message = BuildMessage(method, url, data);
    try
    {
      var response = await _httpClient.SendAsync(message);
      return await ReadResponse<TResponse>(response, message.RequestUri);
    }
    catch (Exception ex)
    {
      ex.Data.Add("Url", $"{_baseUrl}/{url}");
      ex.Data.Add("data", data);
      throw;
    }
  }

  private async Task<ApiResponse<TResponse>> ReadResponse<TResponse>(HttpResponseMessage? response, Uri? requestUri)
      where TResponse : class
  {
    var apiResponse = new ApiResponse<TResponse>();
    if (response != null)
    {
      apiResponse.StatusCode = response.StatusCode;
      apiResponse.Content = response.Content != null ? await response.Content.ReadAsStringAsync() : null;
      if (response.IsSuccessStatusCode && !string.IsNullOrWhiteSpace(apiResponse.Content))
      {
        apiResponse.Model = JsonSerializer.Deserialize<TResponse>(apiResponse.Content, s_readOptions);
      }
      else if (!response.IsSuccessStatusCode)
      {
        _logger.Log(LogLevel.Warning,
            JsonSerializer.Serialize(new { Url = requestUri, apiResponse.StatusCode, apiResponse.Content }, s_logOptions)
        );
      }
    }
    return apiResponse;
  }

  protected void Log(LogLevel logLevel, object message) => _logger.Log(logLevel, JsonSerializer.Serialize(message, s_logOptions));
}

