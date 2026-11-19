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
        public async Task Handler_WhenUserNotFound_ThrowsNotFoundException()
        {
            A.CallTo(() => _userRepository.FindByIdAsync(Guid.Parse("22222222-2222-2222-2222-222222222222"), CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(null));

            Func<Task> act = () => _useCase.Handler(Guid.Parse("22222222-2222-2222-2222-222222222222"), null);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*22222222*");
        }

        [Fact]
        public async Task Handler_WhenSpecificRefreshTokenGiven_DeletesThatToken()
        {
            var user = new UserEntity("t@t.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty("Id")!.SetValue(user, Guid.NewGuid());
            var token = new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "hash",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            A.CallTo(() => _userRepository.FindByIdAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _encryptionService.ComputeSha256Hash("rt-value"))
                .Returns("hash");
            A.CallTo(() => _refreshTokenRepository.FindByTokenHash("hash", CancellationToken.None))
                .Returns(Task.FromResult<RefreshTokenEntity?>(token));

            await _useCase.Handler(Guid.Parse("11111111-1111-1111-1111-111111111111"), "rt-value");

            A.CallTo(() => _refreshTokenRepository.Delete(token.Id)).MustHaveHappened();
        }

        [Fact]
        public async Task Handler_WhenRefreshTokenIsNull_DeletesAllUserTokens()
        {
            var user = new UserEntity("t@t.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty("Id")!.SetValue(user, Guid.NewGuid());

            A.CallTo(() => _userRepository.FindByIdAsync(Guid.Parse("11111111-1111-1111-1111-111111111111"), CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));

            await _useCase.Handler(Guid.Parse("11111111-1111-1111-1111-111111111111"), null);

            A.CallTo(() => _refreshTokenRepository.DeleteByUserId(user.Id)).MustHaveHappened();
        }
    }
}
