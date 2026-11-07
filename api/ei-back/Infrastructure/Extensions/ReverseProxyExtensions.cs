using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace ei_back.Infrastructure.Extensions;

public static class ReverseProxyExtensions
{
    public static IServiceCollection AddTrustedReverseProxy(
        this IServiceCollection services, IConfiguration configuration)
    {
        var addresses = (configuration["ReverseProxy:KnownProxies"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(value => IPAddress.TryParse(value, out var address)
                ? address
                : throw new InvalidOperationException(
                    "ReverseProxy:KnownProxies must contain comma-separated IP addresses."))
            .ToArray();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = 1;
            // Preserve framework loopback trust; never clear the trust lists.
            foreach (var address in addresses)
            {
                options.KnownProxies.Add(address);
            }
        });

        return services;
    }
}
