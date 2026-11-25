namespace ei_back.Core.Application.Service.Sparks;

public interface ISparkCreditsClient
{
    Task<decimal?> GetBalanceAsync(CancellationToken cancellationToken);
}
