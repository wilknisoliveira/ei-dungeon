using ei_back.Core.Application.UseCase.User.Interfaces;
using System.Security.Claims;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using Microsoft.IdentityModel.Tokens;

namespace ei_back.Core.Application.UseCase.User
{
    public class GetUserNameUseCase(ILogger<GetUserNameUseCase> logger) : IGetUserNameUseCase
    {
        public string Handler(ClaimsPrincipal user)
        {
            var userName = user.Identity?.Name ?? "";

            if (!userName.IsNullOrEmpty()) return userName;
            
            logger.LogError("Something went wrong while attempting to get the user logged credential.");
            throw new InternalServerErrorException(
                "Something went wrong while attempting to get the user logged credential.");

        }
    }
}
