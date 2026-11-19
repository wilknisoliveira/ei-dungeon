using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Domain.Entity;
namespace ei_back.Core.Application.Service.Auth.Interfaces;

public interface IApplicationSessionService
{
    TokenDtoReponse Create(User user);
}
