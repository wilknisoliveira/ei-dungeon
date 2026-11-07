using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using ei_back.Infrastructure.Token;
using System.Security.Claims;
using UserEntity = ei_back.Core.Domain.Entity.User;
using RefreshTokenEntity = ei_back.Core.Domain.Entity.RefreshToken;

namespace ei_back.Tests.Core.Application.UseCase.User
{
    public class RefreshTokenUseCaseTests
    {
        private readonly ITokenService _tokenService;
        private readonly IUserRepository _userRepository;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly TokenConfiguration _tokenConfiguration;
        private readonly IRefreshTokenUseCase _useCase;

        public RefreshTokenUseCaseTests()
        {
            _tokenService = A.Fake<ITokenService>();
            _userRepository = A.Fake<IUserRepository>();
            _refreshTokenRepository = A.Fake<IRefreshTokenRepository>();
            _encryptionService = A.Fake<IEncryptionService>();
            _tokenConfiguration = new TokenConfiguration
            {
                Audience = "test",
                Issuer = "test",
                Secret = "this-is-a-very-long-secret-key-for-testing-purposes",
                Minutes = 60,
                DaysToExpiry = 7
            };

            _useCase = new RefreshTokenUseCase(
                _tokenService,
                _userRepository,
                _refreshTokenRepository,
                _tokenConfiguration,
                _encryptionService);
        }

        [Fact]
        public void UnverifiedUser_CannotRefresh()
        {
            var user = new UserEntity("tester", "Test", "test@example.com", "hash", UserRole.CommonUser);
            A.CallTo(() => _tokenService.GetPrincipalFromExpiredToken("token"))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "tester") })));
            A.CallTo(() => _userRepository.FindByUserName("tester", CancellationToken.None)).Returns(user);
            Action act = () => _useCase.Handler(new() { AccessToken = "token", RefreshToken = "refresh" });
            act.Should().Throw<UnauthorizedException>();
            A.CallTo(() => _tokenService.GenerateAccessToken(A<IEnumerable<Claim>>._)).MustNotHaveHappened();
        }
        [Fact]
        public void Handler_WhenPrincipalNameIsNull_ThrowsUnauthorizedException()
        {
            var request = new RefreshTokenDtoRequest { AccessToken = "token", RefreshToken = "rt" };

            A.CallTo(() => _tokenService.GetPrincipalFromExpiredToken("token"))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity()));

            Action act = () => _useCase.Handler(request);

            act.Should().Throw<UnauthorizedException>().WithMessage("Invalid credentials.");
        }

        [Fact]
        public void Handler_WhenUserNotFound_ThrowsUnauthorizedException()
        {
            var request = new RefreshTokenDtoRequest { AccessToken = "token", RefreshToken = "rt" };

            A.CallTo(() => _tokenService.GetPrincipalFromExpiredToken("token"))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "unknown") })));
            A.CallTo(() => _userRepository.FindByUserName("unknown", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(null));

            Action act = () => _useCase.Handler(request);

            act.Should().Throw<UnauthorizedException>().WithMessage("Invalid credentials.");
        }

        [Fact]
        public void Handler_WhenRefreshTokenExpired_ThrowsUnauthorizedException()
        {
            var request = new RefreshTokenDtoRequest { AccessToken = "token", RefreshToken = "old-rt" };
            var user = new UserEntity("testuser", "Test", "t@t.com", "hash", UserRole.CommonUser);
            user.EmailVerifiedAt = DateTimeOffset.UtcNow;
            var expiredToken = new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "hash",
                ExpiresAt = DateTime.UtcNow.AddDays(-1)
            };

            A.CallTo(() => _tokenService.GetPrincipalFromExpiredToken("token"))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") })));
            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _encryptionService.ComputeSha256Hash("old-rt"))
                .Returns("hash");
            A.CallTo(() => _refreshTokenRepository.FindByTokenHash("hash", CancellationToken.None))
                .Returns(Task.FromResult<RefreshTokenEntity?>(expiredToken));

            Action act = () => _useCase.Handler(request);

            act.Should().Throw<UnauthorizedException>().WithMessage("Invalid credentials.");
        }

        [Fact]
        public void Handler_WhenValidRequest_ReturnsNewTokenPair()
        {
            var request = new RefreshTokenDtoRequest { AccessToken = "token", RefreshToken = "old-rt" };
            var user = new UserEntity("testuser", "Test", "t@t.com", "hash", UserRole.CommonUser);
            user.EmailVerifiedAt = DateTimeOffset.UtcNow;
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty("Id")!.SetValue(user, Guid.NewGuid());
            var storedToken = new RefreshTokenEntity
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = "hash",
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            A.CallTo(() => _tokenService.GetPrincipalFromExpiredToken("token"))
                .Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "testuser") })));
            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _encryptionService.ComputeSha256Hash("old-rt"))
                .Returns("hash");
            A.CallTo(() => _refreshTokenRepository.FindByTokenHash("hash", CancellationToken.None))
                .Returns(Task.FromResult<RefreshTokenEntity?>(storedToken));
            A.CallTo(() => _tokenService.GenerateAccessToken(A<IEnumerable<Claim>>._))
                .Returns("new-access-token");
            A.CallTo(() => _tokenService.GenerateRefreshToken())
                .Returns("new-refresh-token");
            A.CallTo(() => _encryptionService.ComputeSha256Hash("new-refresh-token"))
                .Returns("new-hash");

            var result = _useCase.Handler(request);

            result.Should().NotBeNull();
            result.Authenticated.Should().BeTrue();
            result.AccessToken.Should().Be("new-access-token");
            result.RefreshToken.Should().Be("new-refresh-token");
            A.CallTo(() => _refreshTokenRepository.Delete(storedToken.Id)).MustHaveHappened();
        }
    }
}
