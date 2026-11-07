using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;

namespace ei_back.Core.Application.UseCase.User
{
    public class DeleteUserUseCase(IUserRepository userRepository) : IDeleteUserUseCase
    {
        private readonly IUserRepository _userRepository = userRepository;

        public async Task Handler(Guid userId, CancellationToken cancellationToken = default)
        {
            var user = await _userRepository.FindByIdAsync(userId, cancellationToken)
                ?? throw new NotFoundException($"No user with id '{userId}' found.");

            _userRepository.Delete(userId);
        }
    }
}
