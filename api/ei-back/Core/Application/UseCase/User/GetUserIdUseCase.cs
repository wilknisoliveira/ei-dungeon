using System.Security.Claims;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.User;

public class GetUserIdUseCase : IGetUserIdUseCase
{
    public Guid Handler(ClaimsPrincipal user)
    {
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
        if (!Guid.TryParse(subject, out var userId) || userId == Guid.Empty)
            throw new UnauthorizedException("Invalid credentials.");
        return userId;
    }
}
