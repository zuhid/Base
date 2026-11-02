using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.Swagger;
using System.Security.Cryptography;

namespace Zuhid.Base.Tests;

// AddServices writes the static QueryRequest.MaxTake; same collection as QueryRequestTests to avoid races.
[Collection("MaxTake")]
public class WebApplicationExtensionTests
{
    private sealed class TestSetting(IConfiguration configuration) : SettingBase(configuration);

    private static TestSetting BuildSetting(Dictionary<string, string?>? extra = null)
    {
        var values = new Dictionary<string, string?> { ["Name"] = "TestApp" };
        if (extra != null)
        {
            foreach (var kv in extra)
            {
                values[kv.Key] = kv.Value;
            }
        }
        var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        return new TestSetting(config);
    }

    [Fact]
    public void AddServices_ShouldRegisterExpectedServices()
    {
        var builder = WebApplication.CreateBuilder();
        var setting = BuildSetting();
        builder.AddServices(setting);

        var services = builder.Services;
        Assert.Contains(services, s => s.ServiceType == typeof(TestSetting));
        Assert.Contains(services, s => s.ServiceType == typeof(IActionDescriptorCollectionProvider));
        Assert.Contains(services, s => s.ServiceType == typeof(ISwaggerProvider));
        Assert.Contains(services, s => s.ServiceType == typeof(ICorsService));
    }

    [Fact]
    public void AddServices_Should_ApplyMaxTakeFromSetting()
    {
        var original = Zuhid.Base.Query.QueryRequest.MaxTake;
        try
        {
            var builder = WebApplication.CreateBuilder();
            var setting = BuildSetting(new Dictionary<string, string?> { ["MaxTake"] = "70" });
            builder.AddServices(setting);

            Assert.Equal(70, Zuhid.Base.Query.QueryRequest.MaxTake);
        }
        finally
        {
            Zuhid.Base.Query.QueryRequest.MaxTake = original;
        }
    }

    [Fact]
    public void AddServices_Should_ConfigureAuthenticationSchemes()
    {
        var builder = WebApplication.CreateBuilder();
        var setting = BuildSetting();
        builder.AddServices(setting);

        var sp = builder.Services.BuildServiceProvider();
        var authOptions = sp.GetRequiredService<IOptions<AuthenticationOptions>>().Value;

        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultAuthenticateScheme);
        Assert.Equal(JwtBearerDefaults.AuthenticationScheme, authOptions.DefaultChallengeScheme);
    }

    [Fact]
    public async Task AddServices_JwtBearerOptions_And_Events_Should_Configure()
    {
        using var rsa = RSA.Create(2048);
        var pubKeyPath = Path.GetTempFileName();
        File.WriteAllText(pubKeyPath, rsa.ExportSubjectPublicKeyInfoPem());

        try
        {
            var builder = WebApplication.CreateBuilder();
            var setting = BuildSetting(new()
            {
                ["Jwt:PublicKeyPath"] = pubKeyPath,
                ["Jwt:Issuer"] = "https://issuer.test",
                ["Jwt:Audience"] = "api-audience",
                ["Jwt:ExpiryInMinutes"] = "60"
            });
            builder.AddServices(setting);

            var sp = builder.Services.BuildServiceProvider();

            // Resolving JwtBearerOptions triggers the AddJwtBearer lambda (lines 29-43)
            var jwtOptions = sp.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            Assert.True(jwtOptions.TokenValidationParameters.ValidateIssuer);
            Assert.Equal("https://issuer.test", jwtOptions.TokenValidationParameters.ValidIssuer);
            Assert.Equal("api-audience", jwtOptions.TokenValidationParameters.ValidAudience);

            // Directly invoke each JWT event to cover the event-handler lambdas (lines 44-69)
            var eventServices = new ServiceCollection().AddLogging().BuildServiceProvider();
            var httpCtx = new DefaultHttpContext { RequestServices = eventServices };
            var scheme = new AuthenticationScheme(
                JwtBearerDefaults.AuthenticationScheme, null, typeof(JwtBearerHandler));
            var events = jwtOptions.Events;

            await events.TokenValidated(new TokenValidatedContext(httpCtx, scheme, jwtOptions));

            await events.AuthenticationFailed(new AuthenticationFailedContext(httpCtx, scheme, jwtOptions)
            {
                Exception = new Exception("simulated failure")
            });

            await events.Challenge(new JwtBearerChallengeContext(
                httpCtx, scheme, jwtOptions, new AuthenticationProperties())
            {
                Error = "invalid_token",
                ErrorDescription = "Expired",
                AuthenticateFailure = new Exception("expired")
            });

            await events.Forbidden(new ForbiddenContext(
                httpCtx, scheme, jwtOptions));

            // OnMessageReceived: a /hubs request with an access_token query param has its token lifted.
            var hubCtx = new DefaultHttpContext { RequestServices = eventServices };
            hubCtx.Request.Path = "/hubs/chat";
            hubCtx.Request.QueryString = new QueryString("?access_token=hub-token");
            var hubMessage = new MessageReceivedContext(hubCtx, scheme, jwtOptions);
            await events.MessageReceived(hubMessage);
            Assert.Equal("hub-token", hubMessage.Token);

            // A non-hub request leaves the token untouched (the guard's false branch).
            var apiCtx = new DefaultHttpContext { RequestServices = eventServices };
            apiCtx.Request.Path = "/api/values";
            apiCtx.Request.QueryString = new QueryString("?access_token=ignored");
            var apiMessage = new MessageReceivedContext(apiCtx, scheme, jwtOptions);
            await events.MessageReceived(apiMessage);
            Assert.Null(apiMessage.Token);
        }
        finally
        {
            File.Delete(pubKeyPath);
        }
    }

    private sealed class SampleHub : Hub;

    [Fact]
    public void MapHubsByConvention_Should_MapHubEndpointFromTypeName()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSignalR();
        builder.Services.AddAuthorization();
        var app = builder.Build();

        app.MapHubsByConvention(typeof(WebApplicationExtensionTests).Assembly);

        // Endpoints registered via MapHub land on the app's route builder data sources,
        // not the DI EndpointDataSource composite (which only materializes when the app runs).
        var endpoints = ((IEndpointRouteBuilder)app).DataSources.SelectMany(ds => ds.Endpoints);
        Assert.Contains(endpoints, e => (e as RouteEndpoint)?.RoutePattern.RawText == "/hubs/sample");
    }

    [Fact]
    public void AddServices_Should_ConfigureSwaggerGen()
    {
        var builder = WebApplication.CreateBuilder();
        var setting = BuildSetting();
        builder.AddServices(setting);

        var sp = builder.Services.BuildServiceProvider();
        var swaggerProvider = sp.GetRequiredService<ISwaggerProvider>();

        // GetSwagger triggers the AddSwaggerGen lambda (lines 94-107)
        var doc = swaggerProvider.GetSwagger("v1");

        Assert.NotNull(doc);
    }

    [Fact]
    public void BuildServices_ShouldReturnWebApplication()
    {
        var builder = WebApplication.CreateBuilder();
        var setting = BuildSetting();
        builder.AddServices(setting);

        var app = builder.BuildServices(setting);

        Assert.NotNull(app);
    }

    private sealed class FooService;
    private sealed class FooRepository;
    private abstract class AbstractRepository;
    private sealed class UnrelatedClass;

    [Fact]
    public void AddScopedByConvention_ShouldRegisterHttpClientAndMatchingTypes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddScopedByConvention(typeof(WebApplicationExtensionTests).Assembly, ["Service", "Repository"]);

        var services = builder.Services;
        Assert.Contains(services, s => s.ServiceType == typeof(HttpClient));
        Assert.Contains(services, s => s.ServiceType == typeof(FooService));
        Assert.Contains(services, s => s.ServiceType == typeof(FooRepository));
    }

    [Fact]
    public void AddScopedByConvention_ShouldNotRegisterAbstractOrNonMatchingTypes()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddScopedByConvention(typeof(WebApplicationExtensionTests).Assembly, ["Service", "Repository"]);

        var services = builder.Services;
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(AbstractRepository));
        Assert.DoesNotContain(services, s => s.ServiceType == typeof(UnrelatedClass));
    }

    [Fact]
    public void AddScopedByConvention_ShouldRegisterWithScopedLifetime()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddScopedByConvention(typeof(WebApplicationExtensionTests).Assembly, ["Service"]);

        var descriptor = builder.Services.First(s => s.ServiceType == typeof(FooService));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddScopedByConvention_WithEmptySuffixes_ShouldOnlyRegisterHttpClient()
    {
        var builder = WebApplication.CreateBuilder();
        builder.AddScopedByConvention(typeof(WebApplicationExtensionTests).Assembly, []);

        Assert.Contains(builder.Services, s => s.ServiceType == typeof(HttpClient));
        Assert.DoesNotContain(builder.Services, s => s.ServiceType == typeof(FooService));
    }

    [Fact]
    public void AddPostgres_ShouldRegisterDbContext()
    {
        var builder = WebApplication.CreateBuilder();
        var connectionString = "Host=localhost;Database=testdb;Username=postgres;Password=password";

        builder.AddPostgres<TestDbContext>(connectionString);

        var services = builder.Services;
        Assert.Contains(services, s => s.ServiceType == typeof(TestDbContext));

        var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<TestDbContext>();
        Assert.NotNull(dbContext);
    }
}
