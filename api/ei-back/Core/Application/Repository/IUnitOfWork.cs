using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;

namespace ei_back.Core.Application.Repository
{
    public interface IUnitOfWork : IDisposable
    {
        void Commit();
        Task<int> CommitAsync(CancellationToken cancellationToken = default);
        void Rollback();
        /// <summary>Saves and commits the operation on success; rolls back and clears tracking on failure.</summary>
        /// <remarks>Nested transactions and calling Commit/CommitAsync/Rollback inside the operation are rejected.</remarks>
        Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
        IRepository<T> GetRepository<T>() where T : Base;
    }
}
