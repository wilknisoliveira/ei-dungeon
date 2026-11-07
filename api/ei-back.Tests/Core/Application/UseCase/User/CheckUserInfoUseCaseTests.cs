using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.User
{
    public class CheckUserInfoUseCaseTests
    {
        private readonly IUserRepository _userRepository;
        private readonly ICheckUserInfoUseCase _useCase;

        public CheckUserInfoUseCaseTests()
        {
            _userRepository = A.Fake<IUserRepository>();
            _useCase = new CheckUserInfoUseCase(_userRepository);
        }

        [Fact]
        public async Task Handler_WhenBothParamsNull_ReturnsBothAvailable()
        {
            var result = await _useCase.Handler(null, null);

            result.UsernameAvailable.Should().BeTrue();
            result.EmailAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task Handler_WhenUsernameExists_ReturnsUsernameNotAvailable()
        {
            var user = new UserEntity("existing", "Existing", "e@e.com", "hash", UserRole.CommonUser);

            A.CallTo(() => _userRepository.FindByUserName("existing", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));

            var result = await _useCase.Handler("existing", null);

            result.UsernameAvailable.Should().BeFalse();
            result.EmailAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task Handler_WhenEmailExists_ReturnsEmailNotAvailable()
        {
            var user = new UserEntity("existing", "Existing", "taken@t.com", "hash", UserRole.CommonUser);

            A.CallTo(() => _userRepository.FindByEmail("taken@t.com", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));

            var result = await _useCase.Handler(null, "taken@t.com");

            result.UsernameAvailable.Should().BeTrue();
            result.EmailAvailable.Should().BeFalse();
        }

        [Fact]
        public async Task Handler_WhenBothExist_ReturnsBothNotAvailable()
        {
            var usernameUser = new UserEntity("existing", "Existing", "e@e.com", "hash", UserRole.CommonUser);
            var emailUser = new UserEntity("other", "Other", "taken@t.com", "hash", UserRole.CommonUser);

            A.CallTo(() => _userRepository.FindByUserName("existing", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(usernameUser));
            A.CallTo(() => _userRepository.FindByEmail("taken@t.com", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(emailUser));

            var result = await _useCase.Handler("existing", "taken@t.com");

            result.UsernameAvailable.Should().BeFalse();
            result.EmailAvailable.Should().BeFalse();
        }
    }
}
