using ei_back.Core.Application.Repository;
namespace ei_back.Tests.Core.Application.UseCase.User;
internal class TestUnitOfWork : IUnitOfWork
{
    public void Commit() => throw new NotSupportedException();
    public Task<int> CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Committed = true;
        return Task.FromResult(1);
    }
    public void Rollback() => throw new NotSupportedException();
    public void Dispose() { }
    public IRepository<T> GetRepository<T>() where T : ei_back.Core.Domain.Entity.Base => throw new NotSupportedException();
    public bool Committed { get; private set; }
    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        var result = await action(cancellationToken);
        Committed = true;
        return result;
    }
}