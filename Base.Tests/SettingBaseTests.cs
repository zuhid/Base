using Microsoft.Extensions.Configuration;

namespace Zuhid.Base.Tests;

public class SettingBaseTests
{
    private sealed class ConcreteSetting(IConfiguration configuration) : SettingBase(configuration);

    private static IConfiguration BuildConfig(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Fact]
    public void Constructor_Should_BindNameAndVersion()
    {
        var config = BuildConfig(new() { ["Name"] = "MyService", ["Version"] = "v2" });

        var setting = new ConcreteSetting(config);

        Assert.Equal("MyService", setting.Name);
        Assert.Equal("v2", setting.Version);
    }

    [Fact]
    public void Version_Should_DefaultToV1_WhenNotConfigured()
    {
        var config = BuildConfig(new() { ["Name"] = "TestService" });

        var setting = new ConcreteSetting(config);

        Assert.Equal("v1", setting.Version);
    }

    [Fact]
    public void JwtOptions_Properties_Should_SetAndGet()
    {
        var jwt = new SettingBase.JwtOptions
        {
            PrivateKeyPath = "/keys/private.pem",
            PublicKeyPath = "/keys/public.pem",
            Issuer = "https://issuer.example.com",
            Audience = "my-audience",
            ExpiryInMinutes = 120
        };

        Assert.Equal("/keys/private.pem", jwt.PrivateKeyPath);
        Assert.Equal("/keys/public.pem", jwt.PublicKeyPath);
        Assert.Equal("https://issuer.example.com", jwt.Issuer);
        Assert.Equal("my-audience", jwt.Audience);
        Assert.Equal(120, jwt.ExpiryInMinutes);
    }

    [Fact]
    public void Constructor_Should_BindJwtOptions()
    {
        var config = BuildConfig(new()
        {
            ["Name"] = "JwtService",
            ["Jwt:Issuer"] = "https://auth.example.com",
            ["Jwt:Audience"] = "api-clients",
            ["Jwt:ExpiryInMinutes"] = "60"
        });

        var setting = new ConcreteSetting(config);

        Assert.NotNull(setting.Jwt);
        Assert.Equal("https://auth.example.com", setting.Jwt.Issuer);
        Assert.Equal("api-clients", setting.Jwt.Audience);
        Assert.Equal(60, setting.Jwt.ExpiryInMinutes);
    }
}
