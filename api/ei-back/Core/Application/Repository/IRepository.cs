using ei_back.Core.Domain.Entity;

namespace ei_back.Core.Application.Repository
{
    public interface IRepository<T> where T : Base
    {
        T Create(T item);
        Task<T> CreateAsync(T item, CancellationToken cancellationToken = default);
        Task<List<T>> CreateRangeAsync(List<T> items, CancellationToken cancellationToken);
        List<T> FindAll();
        Task<List<T>> FindAllAsync(CancellationToken cancellationToken = default);
        T FindById(Guid id);
        Task<T> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
        T Update(T item);
        void Delete(Guid id);
        bool Exists(Guid id);
        Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    }
}
