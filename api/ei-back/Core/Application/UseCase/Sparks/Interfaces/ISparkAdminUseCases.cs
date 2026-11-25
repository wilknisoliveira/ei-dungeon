using ei_back.Core.Application.UseCase.Sparks.Dtos;

namespace ei_back.Core.Application.UseCase.Sparks.Interfaces;

public interface IGetSparkAdministrationUseCase
{
    Task<SparkAdminResponse> Handler(Guid? cycleId, CancellationToken cancellationToken);
}

public interface IUpdateSparkSettingsUseCase
{
    Task Handler(SparkSettingsRequest request, CancellationToken cancellationToken);
}

public interface IIncreaseSparkCapacityUseCase
{
    Task Handler(Guid cycleId, Guid adminId, int amount, CancellationToken cancellationToken);
}

public interface IResetSparkCycleUseCase
{
    Task<bool> Handler(Guid cycleId, Guid adminId, CancellationToken cancellationToken);
}
