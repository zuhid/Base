using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Zuhid.Base.Tests;

public class TestApiClientOptions : IApiClientOptions
{
    public string BaseUrl { get; set; } = "https://api.test.com";
    public string Authorization { get; set; } = "";
}

public class SimpleRequest
{
    public string Value { get; set; } = string.Empty;
}

public class SimpleResponse
{
    public string Value { get; set; } = string.Empty;
}

public class TestApiClient(HttpClient httpClient, IApiClientOptions options, ILogger logger, bool loginResult = true)
    : ApiClient(httpClient, options, logger)
{
    public override Task<bool> Login() => Task.FromResult(loginResult);

    public void AddCustomHeader(string key, string value) =>
        _customHeaders.Add(new KeyValuePair<string, string>(key, value));

    public void PublicLog(LogLevel logLevel, object message) => Log(logLevel, message);
}

public class MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
{
    public MockHttpMessageHandler(HttpResponseMessage response) : this(_ => response) { }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        Task.FromResult(responseFactory(request));
}

public class ThrowingHttpMessageHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
        throw new HttpRequestException("Simulated connection failure");
}

public class ApiClientTests
{
    private static TestApiClient CreateClient(HttpResponseMessage response, bool loginResult = true)
    {
        var httpClient = new HttpClient(new MockHttpMessageHandler(response));
        var loggerMock = new Mock<ILogger>();
        return new TestApiClient(httpClient, new TestApiClientOptions(), loggerMock.Object, loginResult);
    }

    private static TestApiClient CreateThrowingClient()
    {
        var httpClient = new HttpClient(new ThrowingHttpMessageHandler());
        var loggerMock = new Mock<ILogger>();
        return new TestApiClient(httpClient, new TestApiClientOptions(), loggerMock.Object);
    }

    [Fact]
    public async Task Get_LoginFails_Should_ReturnEmptyResponse()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.OK), loginResult: false);

        var result = await client.Get<SimpleRequest, SimpleResponse>("endpoint", new SimpleRequest());

        Assert.Equal(default, result.StatusCode);
        Assert.Null(result.Model);
        Assert.Null(result.Content);
    }

    [Fact]
    public async Task Get_Success_Should_DeserializeModel()
    {
        var body = JsonSerializer.Serialize(new SimpleResponse { Value = "hello" });
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var client = CreateClient(response);

        var result = await client.Get<SimpleRequest, SimpleResponse>("endpoint", new SimpleRequest());

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Model);
        Assert.Equal("hello", result.Model.Value);
    }

    [Fact]
    public async Task Get_SuccessEmptyBody_Should_ReturnNullModel()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };
        var client = CreateClient(response);

        var result = await client.Get<SimpleRequest, SimpleResponse>("endpoint", new SimpleRequest());

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Null(result.Model);
    }

    [Fact]
    public async Task Get_FailureResponse_Should_LogWarning_And_ReturnNullModel()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request", Encoding.UTF8, "text/plain")
        };
        var client = CreateClient(response);

        var result = await client.Get<SimpleRequest, SimpleResponse>("endpoint", new SimpleRequest());

        Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        Assert.Null(result.Model);
    }

    [Fact]
    public async Task Get_HttpException_Should_RethrowWithData()
    {
        var client = CreateThrowingClient();

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.Get<SimpleRequest, SimpleResponse>("endpoint", new SimpleRequest()));

        Assert.True(ex.Data.Contains("Url"));
        Assert.True(ex.Data.Contains("data"));
    }

    [Fact]
    public async Task Post_WithResponse_Success_Should_DeserializeModel()
    {
        var body = JsonSerializer.Serialize(new SimpleResponse { Value = "posted" });
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var client = CreateClient(response);

        var result = await client.Post<SimpleRequest, SimpleResponse>("endpoint", new SimpleRequest { Value = "data" });

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Model);
        Assert.Equal("posted", result.Model.Value);
    }

    [Fact]
    public async Task Post_WithResponse_LoginFails_Should_ReturnEmpty()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.OK), loginResult: false);

        var result = await client.Post<SimpleRequest, SimpleResponse>("endpoint", new SimpleRequest());

        Assert.Equal(default, result.StatusCode);
        Assert.Null(result.Model);
    }

    [Fact]
    public async Task Put_Success_Should_DeserializeModel()
    {
        var body = JsonSerializer.Serialize(new SimpleResponse { Value = "updated" });
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        var client = CreateClient(response);

        var result = await client.Put<SimpleRequest, SimpleResponse>("endpoint", new SimpleRequest());

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.NotNull(result.Model);
        Assert.Equal("updated", result.Model.Value);
    }

    [Fact]
    public async Task PostVoid_LoginFails_Should_ReturnEarly()
    {
        var client = CreateClient(new HttpResponseMessage(HttpStatusCode.OK), loginResult: false);

        await client.Post<SimpleRequest>("endpoint", new SimpleRequest());
        // no exception = pass
    }

    [Fact]
    public async Task PostVoid_Success_Should_LogInformation()
    {
        var loggerMock = new Mock<ILogger>();
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("", Encoding.UTF8, "text/plain")
        };
        var httpClient = new HttpClient(new MockHttpMessageHandler(response));
        var client = new TestApiClient(httpClient, new TestApiClientOptions(), loggerMock.Object);

        await client.Post<SimpleRequest>("endpoint", new SimpleRequest());

        loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task PostVoid_FailureResponse_Should_LogWarning()
    {
        var loggerMock = new Mock<ILogger>();
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("error", Encoding.UTF8, "text/plain")
        };
        var httpClient = new HttpClient(new MockHttpMessageHandler(response));
        var client = new TestApiClient(httpClient, new TestApiClientOptions(), loggerMock.Object);

        await client.Post<SimpleRequest>("endpoint", new SimpleRequest());

        loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task PostVoid_HttpException_Should_RethrowWithData()
    {
        var client = CreateThrowingClient();

        var ex = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.Post<SimpleRequest>("endpoint", new SimpleRequest()));

        Assert.True(ex.Data.Contains("Url"));
        Assert.True(ex.Data.Contains("data"));
    }

    [Fact]
    public async Task Get_WithCustomHeader_Should_AddToRequest()
    {
        HttpRequestMessage? captured = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("", Encoding.UTF8, "application/json")
            };
        });
        var loggerMock = new Mock<ILogger>();
        var client = new TestApiClient(new HttpClient(handler), new TestApiClientOptions(), loggerMock.Object);
        client.AddCustomHeader("X-Test-Header", "test-value");

        await client.Get<SimpleRequest, SimpleResponse>("endpoint", new SimpleRequest());

        Assert.NotNull(captured);
        Assert.True(captured!.Headers.Contains("X-Test-Header"));
    }

    [Fact]
    public async Task Get_WithDuplicateCustomHeader_Should_ReplaceExistingValue()
    {
        HttpRequestMessage? captured = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("", Encoding.UTF8, "application/json")
            };
        });
        var loggerMock = new Mock<ILogger>();
        var client = new TestApiClient(new HttpClient(handler), new TestApiClientOptions(), loggerMock.Object);
        client.AddCustomHeader("X-Dup", "first");
        client.AddCustomHeader("X-Dup", "second"); // triggers the remove-then-add path

        await client.Get<SimpleRequest, SimpleResponse>("endpoint", new SimpleRequest());

        Assert.NotNull(captured);
        var values = captured!.Headers.GetValues("X-Dup").ToList();
        Assert.Single(values);
        Assert.Equal("second", values[0]);
    }

    [Fact]
    public async Task PostVoid_WithCustomHeader_Should_AddHeaderToRequest()
    {
        HttpRequestMessage? captured = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("", Encoding.UTF8, "text/plain")
            };
        });
        var loggerMock = new Mock<ILogger>();
        var client = new TestApiClient(new HttpClient(handler), new TestApiClientOptions(), loggerMock.Object);
        client.AddCustomHeader("X-Void-Header", "void-value");

        await client.Post<SimpleRequest>("endpoint", new SimpleRequest());

        Assert.NotNull(captured);
        Assert.True(captured!.Headers.Contains("X-Void-Header"));
    }

    [Fact]
    public async Task PostVoid_WithDuplicateCustomHeader_Should_ReplaceExistingValue()
    {
        HttpRequestMessage? captured = null;
        var handler = new MockHttpMessageHandler(req =>
        {
            captured = req;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("", Encoding.UTF8, "text/plain")
            };
        });
        var loggerMock = new Mock<ILogger>();
        var client = new TestApiClient(new HttpClient(handler), new TestApiClientOptions(), loggerMock.Object);
        client.AddCustomHeader("X-Void-Dup", "first");
        client.AddCustomHeader("X-Void-Dup", "second");

        await client.Post<SimpleRequest>("endpoint", new SimpleRequest());

        Assert.NotNull(captured);
        var values = captured!.Headers.GetValues("X-Void-Dup").ToList();
        Assert.Single(values);
        Assert.Equal("second", values[0]);
    }

    [Fact]
    public void Log_Should_CallUnderlyingLogger()
    {
        var loggerMock = new Mock<ILogger>();
        var httpClient = new HttpClient(new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var client = new TestApiClient(httpClient, new TestApiClientOptions(), loggerMock.Object);

        client.PublicLog(LogLevel.Debug, new { Message = "test" });

        loggerMock.Verify(l => l.Log(
            LogLevel.Debug,
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }
}
