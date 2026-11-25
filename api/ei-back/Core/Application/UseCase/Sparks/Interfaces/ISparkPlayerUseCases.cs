using ei_back.Core.Application.UseCase.Sparks.Dtos;

namespace ei_back.Core.Application.UseCase.Sparks.Interfaces;

public interface IGetSparkAvailabilityUseCase
{
    Task<SparkAvailabilityResponse> Handler(CancellationToken cancellationToken);
}

public interface IGetSparkStatusUseCase
{
    Task<SparkStatusResponse> Handler(Guid userId, CancellationToken cancellationToken);
}

public interface IRequestSparksUseCase
{
    Task<SparkStatusResponse> Handler(Guid userId, string cultureName, CancellationToken cancellationToken);
}

public interface IDecideSparkOfferUseCase
{
    Task<SparkStatusResponse> Handler(Guid userId, Guid requestId, bool accepted, CancellationToken cancellationToken);
}
