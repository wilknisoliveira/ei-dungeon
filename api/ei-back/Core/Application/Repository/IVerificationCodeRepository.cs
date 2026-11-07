using ei_back.Core.Domain.Entity;
using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.Repository;

public interface IVerificationCodeRepository
{
    Task AcquireLockAsync(string normalizedEmail, CancellationToken cancellationToken = default);
    Task<List<VerificationCode>> GetHistoryAsync(Guid userId, VerificationCodePurpose purpose,
        DateTimeOffset since, CancellationToken cancellationToken = default);
    Task<VerificationCode?> GetLatestAsync(Guid userId, VerificationCodePurpose purpose,
        CancellationToken cancellationToken = default);
    void Add(VerificationCode code);
}