using ei_back.Core.Application.Repository;
using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace ei_back.Infrastructure.Context.Repository;

// Acquire the account lock inside UnitOfWork.ExecuteInTransactionAsync before querying challenge state.
public class VerificationCodeRepository(EIContext context) : IVerificationCodeRepository
{
    public Task AcquireLockAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        if (context.Database.CurrentTransaction == null)
            throw new InvalidOperationException("A verification lock requires an active transaction.");
        return context.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({normalizedEmail}, 0))", cancellationToken);
    }
    public Task<List<VerificationCode>> GetHistoryAsync(Guid userId, VerificationCodePurpose purpose,
        DateTimeOffset since, CancellationToken cancellationToken = default) =>
        context.VerificationCodes.Where(c => c.UserId == userId && c.Purpose == purpose && c.CreatedAt > since)
            .ToListAsync(cancellationToken);

    public Task<VerificationCode?> GetLatestAsync(Guid userId, VerificationCodePurpose purpose,
        CancellationToken cancellationToken = default) =>
        context.VerificationCodes.Where(c => c.UserId == userId && c.Purpose == purpose)
            .OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id).FirstOrDefaultAsync(cancellationToken);

    public void Add(VerificationCode code) => context.VerificationCodes.Add(code);
}