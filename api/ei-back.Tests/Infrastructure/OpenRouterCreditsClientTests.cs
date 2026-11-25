using System.Net;
using System.Text;
using ei_back.Core.Application.Service.Sparks;
using ei_back.Infrastructure.GenAI;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ei_back.Tests.Infrastructure;

public class OpenRouterCreditsClientTests
{
    private sealed class CreditsHandler(HttpStatusCode status, string responseBody) : HttpMessageHandler
    {
        public Uri? RequestUri { get; private set; }
        public string? Token { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUri = request.RequestUri;
            Token = request.Headers.Authorization?.Parameter;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }

    [Fact]
    public async Task GetBalance_WhenCreditsArePresent_SubtractsUsageAndUsesManagementKey()
    {
        using var handler = new CreditsHandler(HttpStatusCode.OK, """{"data":{"total_credits":10.02,"total_usage":5.018}}""");
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://openrouter.ai/api/v1/") };
        var client = new OpenRouterCreditsClient(http, Options.Create(new SparkOptions { OpenRouterManagementKey = "test-key" }),
            NullLogger<OpenRouterCreditsClient>.Instance);

        (await client.GetBalanceAsync(default)).Should().Be(5.002m);
        handler.RequestUri.Should().Be(new Uri("https://openrouter.ai/api/v1/credits"));
        handler.Token.Should().Be("test-key");
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, """{"data":{"total_credits":0}}""")]
    [InlineData(HttpStatusCode.ServiceUnavailable, """{"error":{"message":"provider unavailable"}}""")]
    public async Task GetBalance_WhenObservationFails_ReturnsUnknown(HttpStatusCode status, string body)
    {
        using var handler = new CreditsHandler(status, body);
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://openrouter.ai/api/v1/") };
        var client = new OpenRouterCreditsClient(http, Options.Create(new SparkOptions { OpenRouterManagementKey = "test-key" }),
            NullLogger<OpenRouterCreditsClient>.Instance);

        (await client.GetBalanceAsync(default)).Should().BeNull();
    }
}
