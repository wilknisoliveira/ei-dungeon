using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using UserEntity = ei_back.Core.Domain.Entity.User;
using RefreshTokenEntity = ei_back.Core.Domain.Entity.RefreshToken;

namespace ei_back.Tests.Core.Application.UseCase.User
{
    public class LogoutUseCaseTests
    {
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly ILogoutUseCase _useCase;

        public LogoutUseCaseTests()
        {
            _userRepository = A.Fake<IUserRepository>();
            _refreshTokenRepository = A.Fake<IRefreshTokenRepository>();
            _encryptionService = A.Fake<IEncryptionService>();
            _useCase = new LogoutUseCase(_userRepository, _refreshTokenRepository, _encryptionService);
        }

        [Fact]
        public void Handler_WhenUserNotFound_ThrowsNotFoundException()
        {
            A.CallTo(() => _userRepository.FindByUserName("unknown", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(null));

            Action act = () => _useCase.Handler("unknown", null);

            act.Should().Throw<NotFoundException>().WithMessage("*unknown*");
        }

        [Fact]
        public void Handler_WhenSpecificRefreshTokenGiven_DeletesThatToken()
        {
            var user = new UserEntity("testuser", "Test", "t@t.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty("Id")!.SetValue(user, Guid.NewGuid());
            var token = new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "hash",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _encryptionService.ComputeSha256Hash("rt-value"))
                .Returns("hash");
            A.CallTo(() => _refreshTokenRepository.FindByTokenHash("hash", CancellationToken.None))
                .Returns(Task.FromResult<RefreshTokenEntity?>(token));

            _useCase.Handler("testuser", "rt-value");

            A.CallTo(() => _refreshTokenRepository.Delete(token.Id)).MustHaveHappened();
        }

        [Fact]
        public void Handler_WhenRefreshTokenIsNull_DeletesAllUserTokens()
        {
            var user = new UserEntity("testuser", "Test", "t@t.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty("Id")!.SetValue(user, Guid.NewGuid());

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));

            _useCase.Handler("testuser", null);

            A.CallTo(() => _refreshTokenRepository.DeleteByUserId(user.Id)).MustHaveHappened();
        }
    }
}
