using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Email.Interfaces;
using ei_back.Core.Application.Service.Verification.Interfaces;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.Utils;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Core.Application.Service.Verification;

public class VerificationFlowService(IUserRepository userRepository,
        IVerificationCodeRepository verificationCodeRepository,
        IUnitOfWork unitOfWork,
        IVerificationCodeService verificationCodeService,
        IVerificationCodeProtector verificationCodeProtector,
        IEmailSender emailSender,
        IVerificationEmailComposer verificationEmailComposer,
        ILogger<VerificationFlowService> logger) : IVerificationFlowService
{
    // Existing accounts require the normalized-email transaction/lock. A new, uncommitted account
    // prepares its initial code before the same CommitAsync that saves the account.
    public async Task<CodeDelivery?> PrepareAsync(UserEntity user, VerificationCodePurpose purpose, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var history = await verificationCodeRepository.GetHistoryAsync(user.Id, purpose, now.AddHours(-1), cancellationToken);
        if (!verificationCodeService.CanIssue(history, now)) return null;
        foreach (var old in history.Where(x => x.ConsumedAt == null && x.InvalidatedAt == null))
        {
            old.InvalidatedAt = now;
            old.UpdatedAt = now;
        }
        var record = verificationCodeService.Create(user.Id, user.Email, purpose, now);
        var plain = verificationCodeProtector.GenerateCode();
        record.CodeHash = verificationCodeProtector.Hash(record, plain);
        verificationCodeRepository.Add(record);
        return new CodeDelivery(user.Email, plain, purpose, record.ExpiresAt - record.CreatedAt);
    }

    public async Task DeliverAsync(CodeDelivery? delivery, CancellationToken cancellationToken = default)
    {
        if (delivery == null) return;
        try { await emailSender.SendAsync(verificationEmailComposer.Compose(delivery), cancellationToken); }
        catch (Exception)
        {
            // Provider exception text can contain secrets or message bodies. Do not log it.
            logger.LogError("Verification code delivery failed for purpose {Purpose}. The committed challenge is retained.", delivery.Purpose);
        }
    }

    public async Task RequestAsync(string email, VerificationCodePurpose purpose, CancellationToken cancellationToken = default)
    {
        var normalized = EmailNormalizer.Normalize(email);
        var delivery = await unitOfWork.ExecuteInTransactionAsync<CodeDelivery?>(async ct =>
        {
            await verificationCodeRepository.AcquireLockAsync(normalized, ct);
            var user = await userRepository.FindByEmail(normalized, ct);
            if (user == null || !Eligible(user, purpose)) return null;
            return await PrepareAsync(user, purpose, cancellationToken);
        }, cancellationToken);
        await DeliverAsync(delivery, cancellationToken);
    }

    public Task<CodeConfirmationResult> ConfirmAsync(string email, string code, VerificationCodePurpose purpose,
        Func<UserEntity, Task> apply, CancellationToken cancellationToken = default)
    {
        var normalized = EmailNormalizer.Normalize(email);
        return unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            await verificationCodeRepository.AcquireLockAsync(normalized, ct);
            var user = await userRepository.FindByEmail(normalized, ct);
            if (user == null || !Eligible(user, purpose)) return CodeConfirmationResult.Invalid;
            var record = await verificationCodeRepository.GetLatestAsync(user.Id, purpose, cancellationToken);
            if (record == null) return CodeConfirmationResult.Invalid;
            if (!verificationCodeService.Confirm(record, user.Id, normalized, purpose, DateTimeOffset.UtcNow, verificationCodeProtector.Matches(record, code)))
                return CodeConfirmationResult.Invalid;
            await apply(user);
            user.UpdatedAt = DateTimeOffset.UtcNow;
            return CodeConfirmationResult.Confirmed;
        }, cancellationToken);
    }

    private static bool Eligible(UserEntity user, VerificationCodePurpose purpose) => purpose switch
    {
        VerificationCodePurpose.EmailVerification => user.EmailVerifiedAt == null,
        VerificationCodePurpose.PasswordRecovery => user.EmailVerifiedAt != null,
        _ => false
    };
}