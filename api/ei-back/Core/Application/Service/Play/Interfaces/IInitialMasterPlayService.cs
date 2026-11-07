using ei_back.Core.Application.Interfaces;

namespace ei_back.Core.Application.Service.Play.Interfaces
{
    public interface IInitialMasterPlayService
    {
        IAsyncEnumerable<StreamAIDtoResponse> ExecuteStreamingAsync(
            Domain.Entity.Game game,
            CancellationToken cancellationToken);
    }
}
