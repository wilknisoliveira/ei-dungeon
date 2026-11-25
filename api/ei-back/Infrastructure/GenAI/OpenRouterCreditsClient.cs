using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using ei_back.Core.Application.Service.Sparks;
using Microsoft.Extensions.Options;

namespace ei_back.Infrastructure.GenAI;

public sealed class OpenRouterCreditsClient(
    HttpClient http,
    IOptions<SparkOptions> options,
    ILogger<OpenRouterCreditsClient> logger) : ISparkCreditsClient
{
    public async Task<decimal?> GetBalanceAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(options.Value.OpenRouterManagementKey))
            return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "credits");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.OpenRouterManagementKey);
            using var response = await http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OpenRouter credits query returned HTTP {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            var credits = await response.Content.ReadFromJsonAsync<CreditsResponse>(cancellationToken);
            if (credits?.Data is { TotalCredits: { } purchased, TotalUsage: { } used } && purchased >= 0 && used >= 0)
                return purchased - used;

            logger.LogWarning("OpenRouter credits query returned an incomplete balance.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("OpenRouter credits query timed out.");
        }
        catch (Exception exception) when (exception is HttpRequestException or IOException or JsonException or FormatException)
        {
            logger.LogWarning("OpenRouter credits query failed with {FailureType}.", exception.GetType().Name);
        }

        return null;
    }

    private sealed record CreditsResponse(CreditsData? Data);
    private sealed record CreditsData(
        [property: JsonPropertyName("total_credits")] decimal? TotalCredits,
        [property: JsonPropertyName("total_usage")] decimal? TotalUsage);
}
