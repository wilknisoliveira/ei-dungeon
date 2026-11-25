using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Sparks;
using ei_back.Core.Application.UseCase.Sparks.Dtos;
using ei_back.Core.Application.UseCase.Sparks.Interfaces;
using Microsoft.Extensions.Options;

namespace ei_back.Core.Application.UseCase.Sparks;

public sealed class GetSparkAvailabilityUseCase(ISparkRepository repository, IOptions<SparkOptions> options)
    : IGetSparkAvailabilityUseCase
{
    public async Task<SparkAvailabilityResponse> Handler(CancellationToken cancellationToken)
    {
        var settings = await repository.GetSettingsAsync(cancellationToken);
        return new(settings?.InitialAmount ?? options.Value.InitialAmount);
    }
}

public sealed class GetSparkStatusUseCase(SparkAllocationService allocation) : IGetSparkStatusUseCase
{
    public async Task<SparkStatusResponse> Handler(Guid userId, CancellationToken cancellationToken) =>
        SparkStatusResponse.From(await allocation.StatusAsync(userId, cancellationToken));
}

public sealed class RequestSparksUseCase(SparkAllocationService allocation) : IRequestSparksUseCase
{
    public async Task<SparkStatusResponse> Handler(Guid userId, string cultureName, CancellationToken cancellationToken) =>
        SparkStatusResponse.From(await allocation.RequestAsync(userId, cancellationToken, cultureName));
}

public sealed class DecideSparkOfferUseCase(SparkAllocationService allocation) : IDecideSparkOfferUseCase
{
    public async Task<SparkStatusResponse> Handler(Guid userId, Guid requestId, bool accepted, CancellationToken cancellationToken) =>
        SparkStatusResponse.From(await allocation.DecideAsync(userId, requestId, accepted, cancellationToken));
}
