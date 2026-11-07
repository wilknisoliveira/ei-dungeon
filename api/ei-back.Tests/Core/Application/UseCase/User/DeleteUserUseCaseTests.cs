using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.User
{
    public class DeleteUserUseCaseTests
    {
        private readonly IUserRepository _userRepository;
        private readonly IDeleteUserUseCase _useCase;

        public DeleteUserUseCaseTests()
        {
            _userRepository = A.Fake<IUserRepository>();
            _useCase = new DeleteUserUseCase(_userRepository);
        }

        [Fact]
        public async Task Handler_WhenUserNotFound_ThrowsNotFoundException()
        {
            var userId = Guid.NewGuid();

            A.CallTo(() => _userRepository.FindByIdAsync(userId, CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(null));

            Func<Task> act = () => _useCase.Handler(userId);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage($"*{userId}*");
        }

        [Fact]
        public async Task Handler_WhenUserExists_DeletesUser()
        {
            var userId = Guid.NewGuid();
            var user = new UserEntity("testuser", "Test", "t@t.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty("Id")!.SetValue(user, userId);

            A.CallTo(() => _userRepository.FindByIdAsync(userId, CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));

            await _useCase.Handler(userId);

            A.CallTo(() => _userRepository.Delete(userId)).MustHaveHappenedOnceExactly();
        }
    }
}
