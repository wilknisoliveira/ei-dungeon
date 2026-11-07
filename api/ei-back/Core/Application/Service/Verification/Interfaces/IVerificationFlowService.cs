using ei_back.Core.Domain.Enums;
using ei_back.Core.Application.UseCase.User;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Core.Application.Service.Verification.Interfaces;

public interface IVerificationFlowService
{
    Task<CodeDelivery?> PrepareAsync(UserEntity user, VerificationCodePurpose purpose, CancellationToken cancellationToken = default);
    Task DeliverAsync(CodeDelivery? delivery, CancellationToken cancellationToken = default);
    Task RequestAsync(string email, VerificationCodePurpose purpose, CancellationToken cancellationToken = default);
    Task<CodeConfirmationResult> ConfirmAsync(string email, string code, VerificationCodePurpose purpose,
        Func<UserEntity, Task> apply, CancellationToken cancellationToken = default);
}