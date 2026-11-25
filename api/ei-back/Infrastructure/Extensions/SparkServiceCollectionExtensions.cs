using ei_back.Core.Application.Service.Sparks;
using ei_back.Core.Application.UseCase.Sparks;
using ei_back.Core.Application.UseCase.Sparks.Interfaces;
using ei_back.Infrastructure.GenAI;
using ei_back.Infrastructure.Services;

namespace ei_back.Infrastructure.Extensions;

public static class SparkServiceCollectionExtensions
{
    public static IServiceCollection AddSparkOperations(this IServiceCollection services)
    {
        services.AddScoped<IGetSparkAvailabilityUseCase, GetSparkAvailabilityUseCase>();
        services.AddScoped<IGetSparkStatusUseCase, GetSparkStatusUseCase>();
        services.AddScoped<IRequestSparksUseCase, RequestSparksUseCase>();
        services.AddScoped<IDecideSparkOfferUseCase, DecideSparkOfferUseCase>();
        services.AddScoped<IGetSparkAdministrationUseCase, GetSparkAdministrationUseCase>();
        services.AddScoped<IUpdateSparkSettingsUseCase, UpdateSparkSettingsUseCase>();
        services.AddScoped<IIncreaseSparkCapacityUseCase, IncreaseSparkCapacityUseCase>();
        services.AddScoped<IResetSparkCycleUseCase, ResetSparkCycleUseCase>();
        services.AddScoped<SparkCycleOperations>();
        services.AddScoped<SparkNotifications>();
        services.AddHttpClient<ISparkCreditsClient, OpenRouterCreditsClient>(client =>
        {
            client.BaseAddress = new Uri("https://openrouter.ai/api/v1/");
            client.Timeout = TimeSpan.FromSeconds(15);
        }).RedactLoggedHeaders(["Authorization"]);
        services.AddHostedService<SparkCycleWorker>();
        return services;
    }
}
