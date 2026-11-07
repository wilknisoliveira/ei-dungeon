using System.Net;
using ei_back.Infrastructure.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ei_back.Tests.Infrastructure;

public class ReverseProxyTests
{
    [Theory]
    [InlineData("172.30.80.1", "172.30.80.1", true)]
    [InlineData("172.30.80.1", "::ffff:172.30.80.1", true)]
    [InlineData("172.30.80.1", "198.51.100.5", false)]
    [InlineData(null, "172.30.80.1", false)]
    public async Task ForwardedHeaders_OnlyTrustedProxyCanBypassHttpsRedirect(
        string? trustedProxy, string remoteAddress, bool trusted)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ReverseProxy:KnownProxies"] = trustedProxy }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddTrustedReverseProxy(configuration);
        services.AddHttpsRedirection(options => options.HttpsPort = 443);
        using var provider = services.BuildServiceProvider();
        var builder = new ApplicationBuilder(provider);
        builder.UseForwardedHeaders();
        builder.Use(next => new HttpsRedirectionMiddleware(next,
            provider.GetRequiredService<IOptions<HttpsRedirectionOptions>>(),
            configuration, provider.GetRequiredService<ILoggerFactory>()).Invoke);
        builder.Run(context =>
        {
            context.Response.StatusCode = 204;
            return Task.CompletedTask;
        });
        var context = new DefaultHttpContext();
        context.RequestServices = provider;
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("api.dungeon.wildforges.com");
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.42";

        await builder.Build()(context);

        Assert.Equal(trusted ? 204 : 307, context.Response.StatusCode);
        Assert.Equal(trusted ? "https" : "http", context.Request.Scheme);
        Assert.Equal(IPAddress.Parse(trusted ? "203.0.113.42" : remoteAddress),
            context.Connection.RemoteIpAddress);
    }

    [Fact]
    public void InvalidProxyAddress_FailsConfiguration()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["ReverseProxy:KnownProxies"] = "*" }).Build();

        Assert.Throws<InvalidOperationException>(() =>
            new ServiceCollection().AddTrustedReverseProxy(configuration));
    }
}
