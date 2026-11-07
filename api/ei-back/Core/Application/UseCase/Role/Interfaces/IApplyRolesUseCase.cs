using ei_back.Core.Application.UseCase.Role.Dtos;

namespace ei_back.Core.Application.UseCase.Role.Interfaces
{
    public interface IApplyRolesUseCase
    {
        Task<ApplyRoleDtoResponse> Handler(ApplyRoleDtoRequest applyRoleDtoRequest);
    }
}
