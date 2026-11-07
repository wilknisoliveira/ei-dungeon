using Microsoft.EntityFrameworkCore;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Application.Repository;
using ei_back.Infrastructure.Context.Repository;

namespace ei_back.Infrastructure.Context
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly EIContext _context;
        private bool _executingTransaction;
        private Dictionary<Type, Object> _repositories;

        public UnitOfWork(EIContext dbContext)
        {
            _context = dbContext;
            _repositories = new Dictionary<Type, object>();
        }

        public void Commit()
        {
            EnsureNoExplicitTransaction();
            _context.SaveChanges();
        }

        public async Task<int> CommitAsync(CancellationToken cancellationToken = default)
        {
            EnsureNoExplicitTransaction();
            try
            {
                return await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException { SqlState: "23505" })
            {
                _context.ChangeTracker.Clear();
                throw new PersistenceConflictException();
            }
        }

        public void Rollback()
        {
            EnsureNoExplicitTransaction();
            _context.ChangeTracker.Clear();
        }

        private void EnsureNoExplicitTransaction()
        {
            if (_executingTransaction || _context.Database.CurrentTransaction != null)
                throw new InvalidOperationException("The explicit transaction owns saving and commit. Nested transactions and manual commits are not supported.");
        }

        public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(operation);
            EnsureNoExplicitTransaction();
            _executingTransaction = true;
            try
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var result = await operation(cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch (Exception exception)
                {
                    try { await transaction.RollbackAsync(CancellationToken.None); }
                    catch (Exception rollbackException) { exception.Data["RollbackException"] = rollbackException; }
                    finally { _context.ChangeTracker.Clear(); }
                    if (exception is DbUpdateException { InnerException: Npgsql.PostgresException { SqlState: "23505" } })
                        throw new PersistenceConflictException();
                    throw;
                }
            }
            finally { _executingTransaction = false; }
        }
        public IRepository<T> GetRepository<T>() where T : Base
        {
            if (_repositories.ContainsKey(typeof(T)))
            {
                return (IRepository<T>)_repositories[typeof(T)];
            }

            var repository = new GenericRepository<T>(_context);
            _repositories.Add(typeof(T), repository);
            return repository;
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
