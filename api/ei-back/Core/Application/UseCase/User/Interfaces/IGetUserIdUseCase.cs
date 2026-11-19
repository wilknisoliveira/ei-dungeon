using System.Security.Claims;

namespace ei_back.Core.Application.UseCase.User.Interfaces;

public interface IGetUserIdUseCase
{
    Guid Handler(ClaimsPrincipal user);
}
