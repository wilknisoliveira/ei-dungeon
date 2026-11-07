using Microsoft.AspNetCore.Localization;

namespace ei_back.Infrastructure.Extensions;

public static class LocalizationExtensions
{
    public static IServiceCollection AddApplicationLocalization(this IServiceCollection services)
    {
        services.AddLocalization(options => options.ResourcesPath = "Infrastructure/Resources");
        services.Configure<RequestLocalizationOptions>(options =>
        {
            options.SetDefaultCulture("en")
                .AddSupportedCultures("en", "pt-BR", "es")
                .AddSupportedUICultures("en", "pt-BR", "es");
        });
        return services;
    }
}