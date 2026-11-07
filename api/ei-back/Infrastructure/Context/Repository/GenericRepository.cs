using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using ei_back.Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Infrastructure.Context.Repository
{
    public class GenericRepository<T> : IRepository<T> where T : Base
    {
        protected EIContext _context;
        private DbSet<T> _dbSet;

        public GenericRepository(EIContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public T Create(T item)
        {
            try
            {
                _context.Add(item);
                return item;
            }
            catch
            {
                throw;
            }
        }

        public virtual async Task<T> CreateAsync(T item, CancellationToken cancellationToken = default)
        {
            try
            {
                await _context.AddAsync(item, cancellationToken);
                return item;
            }
            catch
            {
                throw;
            }
        }

        public async Task<List<T>> CreateRangeAsync(List<T> items, CancellationToken cancellationToken)
        {
            try
            {
                await _context.AddRangeAsync(items, cancellationToken);
                return items;
            }
            catch
            {
                throw;
            }
        }

        public List<T> FindAll()
        {
            return _dbSet.ToList();
        }

        public virtual async Task<List<T>> FindAllAsync(CancellationToken cancellationToken = default)
        {
            return await _dbSet.ToListAsync(cancellationToken);
        }

        public T FindById(Guid id)
        {
            return _dbSet.SingleOrDefault(g => g.Id.Equals(id));
        }

        public virtual async Task<T> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.SingleOrDefaultAsync(g => g.Id.Equals(id), cancellationToken);
        }

        public T Update(T item)
        {
            if (!Exists(item.Id)) return null;

            var result = FindById(item.Id);
            if (result != null)
            {
                try
                {
                    _context.Entry(result).CurrentValues.SetValues(item);
                    return result;
                }
                catch (Exception)
                {
                    throw;
                }
            }
            else return null;
        }

        public void Delete(Guid id)
        {
            var result = FindById(id);
            if (result != null)
            {
                try
                {
                    _dbSet.Remove(result);
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        public bool Exists(Guid id)
        {
            return _dbSet.Any(g => g.Id.Equals(id));
        }

        public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return await _dbSet.AnyAsync(g => g.Id.Equals(id), cancellationToken);
        }

    }
}
