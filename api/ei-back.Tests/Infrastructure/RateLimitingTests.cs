using System.Net;
using ei_back.Infrastructure.Extensions;
using ei_back.UserInterface.Api;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ei_back.Tests.Infrastructure;

public class RateLimitingTests
{
    private sealed class Pipeline : IDisposable
    {
        private readonly ServiceProvider _provider;
        private readonly RequestDelegate _pipeline;
        public Pipeline()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            { ["ReverseProxy:KnownProxies"] = "172.30.80.1" }).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddMetrics();
            services.AddTrustedReverseProxy(configuration);
            services.AddRateLimitingPolicies();
            _provider = services.BuildServiceProvider();
            var builder = new ApplicationBuilder(_provider);
            builder.UseForwardedHeaders();
            builder.UseRateLimiter();
            builder.Run(context => { context.Response.StatusCode = 204; return Task.CompletedTask; });
            _pipeline = builder.Build();
        }

        public async Task<HttpContext> Send(string policy, string path, string ip = "198.51.100.1", string? forwarded = null)
        {
            var context = new DefaultHttpContext { RequestServices = _provider };
            context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
            context.Request.Path = path;
            if (forwarded != null) context.Request.Headers["X-Forwarded-For"] = forwarded;
            context.SetEndpoint(new Endpoint(_ => Task.CompletedTask,
                new EndpointMetadataCollection(new EnableRateLimitingAttribute(policy)), path));
            await _pipeline(context);
            return context;
        }
        public void Dispose() => _provider.Dispose();
    }

    [Fact]
    public async Task RequestRoutes_ShareFivePermits_ConfirmationHasIndependentTenPermits()
    {
        using var pipeline = new Pipeline();
        for (var i = 0; i < 5; i++)
            (await pipeline.Send("VerificationRequest", i % 2 == 0 ? "/email-verification" : "/password-recovery")).Response.StatusCode.Should().Be(204);
        var rejected = await pipeline.Send("VerificationRequest", "/password-recovery");
        rejected.Response.StatusCode.Should().Be(429);
        if (rejected.Response.Headers.TryGetValue("Retry-After", out var retryAfter))
            int.Parse(retryAfter.ToString()).Should().BeGreaterThan(0);
        for (var i = 0; i < 10; i++)
            (await pipeline.Send("VerificationConfirm", i % 2 == 0 ? "/email-verification/confirm" : "/password-recovery/confirm")).Response.StatusCode.Should().Be(204);
        (await pipeline.Send("VerificationConfirm", "/email-verification/confirm")).Response.StatusCode.Should().Be(429);
        (await pipeline.Send("VerificationRequest", "/email-verification", "198.51.100.2")).Response.StatusCode.Should().Be(204);
    }

    [Fact]
    public async Task UntrustedForwardedHeader_CannotChangeBuckets_ForNewOrExistingPolicies()
    {
        foreach (var policy in new[] { "VerificationRequest", "Login" })
        {
            using var pipeline = new Pipeline();
            for (var i = 0; i < 5; i++)
                (await pipeline.Send(policy, "/request", forwarded: $"203.0.113.{i + 1}")).Response.StatusCode.Should().Be(204);
            (await pipeline.Send(policy, "/request", forwarded: "203.0.113.99")).Response.StatusCode.Should().Be(429);
        }
    }

    [Fact]
    public async Task TrustedProxy_UsesProcessedAddressForIndependentClientBuckets()
    {
        using var pipeline = new Pipeline();
        for (var i = 0; i < 5; i++)
        {
            var result = await pipeline.Send("VerificationRequest", "/request", "172.30.80.1", "203.0.113.1");
            result.Connection.RemoteIpAddress.Should().Be(IPAddress.Parse("203.0.113.1"));
            result.Response.StatusCode.Should().Be(204);
        }
        (await pipeline.Send("VerificationRequest", "/request", "172.30.80.1", "203.0.113.1")).Response.StatusCode.Should().Be(429);
        (await pipeline.Send("VerificationRequest", "/request", "172.30.80.1", "203.0.113.2")).Response.StatusCode.Should().Be(204);
    }

    [Theory]
    [InlineData(nameof(AuthController.RequestEmailVerification), "VerificationRequest")]
    [InlineData(nameof(AuthController.RequestPasswordRecovery), "VerificationRequest")]
    [InlineData(nameof(AuthController.ConfirmEmailVerification), "VerificationConfirm")]
    [InlineData(nameof(AuthController.ConfirmPasswordRecovery), "VerificationConfirm")]
    public void ActualActions_UseTheirSharedPolicy(string action, string policy)
    {
        var attribute = typeof(AuthController).GetMethod(action)!.GetCustomAttributes(typeof(EnableRateLimitingAttribute), false)
            .Cast<EnableRateLimitingAttribute>().Single();
        attribute.PolicyName.Should().Be(policy);
    }
}