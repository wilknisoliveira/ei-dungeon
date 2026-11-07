using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Repository
{
    public interface IUserRepository : IRepository<User>
    {
        User ValidateCredentials(string userName, string pass);
        User RefreshUserInfo(User user);
        Task<User?> GetUserAndRolesAsync(Guid userId, CancellationToken cancellationToken = default);
        Task<User?> FindByUserName(string userName, CancellationToken cancellationToken = default);
        // Matches normalized email (trimmed, invariant uppercase).
        Task<User?> FindByEmail(string email, CancellationToken cancellationToken = default);
        Task<Dictionary<UserRole, List<string>>> GetUsersNameGroupByRole();
        Task<List<User>> FindWithPagedSearchAsync(string sort, int size, int offset, string? name, CancellationToken cancellationToken = default);
        Task<int> GetCountAsync(string? name, CancellationToken cancellationToken = default);
    }
}
