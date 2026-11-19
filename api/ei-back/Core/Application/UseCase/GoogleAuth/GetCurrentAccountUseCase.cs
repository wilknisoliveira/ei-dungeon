using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Auth.Interfaces;
using ei_back.Infrastructure.Extensions;
using ei_back.Core.Application.UseCase.GoogleAuth.Dtos;
using ei_back.Core.Application.UseCase.GoogleAuth.Interfaces;

namespace ei_back.Core.Application.UseCase.GoogleAuth;

public class GetCurrentAccountUseCase(IExternalIdentityRepository externalIdentityRepository,
    IGoogleAuthFlowService authFlowService) : IGetCurrentAccountUseCase
{
    public async Task<CurrentAccountDtoResponse> Handler(Guid userId, CancellationToken cancellationToken)
    {
        var user = await authFlowService.GetVerifiedUserAsync(userId, cancellationToken);
        var link = await externalIdentityRepository.FindByUserAsync(userId, cancellationToken);
        return new CurrentAccountDtoResponse(user.Id, user.Email, user.Role.GetEnumDescription(),
            !string.IsNullOrEmpty(user.Password), link != null);
    }
}
