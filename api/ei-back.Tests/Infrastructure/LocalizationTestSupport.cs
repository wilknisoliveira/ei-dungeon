using ei_back.Core.Application.Service.Email;
using ei_back.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace ei_back.Tests.Infrastructure;

internal static class LocalizationTestSupport
{
    private static readonly ServiceProvider Provider = CreateProvider();

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationLocalization();
        services.AddTransient<VerificationEmailComposer>();
        return services.BuildServiceProvider();
    }

    public static VerificationEmailComposer CreateComposer() => Provider.GetRequiredService<VerificationEmailComposer>();
}
