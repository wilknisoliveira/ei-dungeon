using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Token;
using System.Security.Claims;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.User
{
    public class SigninUseCaseTests
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenService _tokenService;
        private readonly IEncryptionService _encryptionService;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly TokenConfiguration _tokenConfiguration;
        private readonly ISignInUseCase _useCase;

        public SigninUseCaseTests()
        {
            _userRepository = A.Fake<IUserRepository>();
            _tokenService = A.Fake<ITokenService>();
            _encryptionService = A.Fake<IEncryptionService>();
            _refreshTokenRepository = A.Fake<IRefreshTokenRepository>();
            _tokenConfiguration = new TokenConfiguration
            {
                Audience = "test",
                Issuer = "test",
                Secret = "this-is-a-very-long-secret-key-for-testing-purposes",
                Minutes = 60,
                DaysToExpiry = 7
            };

            _useCase = new SigninUseCase(
                _tokenConfiguration,
                _userRepository,
                _tokenService,
                _encryptionService,
                _refreshTokenRepository);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UnverifiedUser_RequiresCorrectPasswordBeforeDisclosingVerificationState(bool validPassword)
        {
            var user = new UserEntity("tester", "Tester", "test@example.com", "$2a$11$hash", UserRole.CommonUser);
            A.CallTo(() => _userRepository.FindByUserName("tester", CancellationToken.None)).Returns(user);
            A.CallTo(() => _encryptionService.VerifyBcryptHash("password", user.Password)).Returns(validPassword);
            if (validPassword)
            {
                Action act = () => _useCase.Handler(new() { UserName = "tester", Password = "password" });
                act.Should().Throw<ei_back.Core.Domain.DomainExceptions.EmailVerificationRequiredException>();
            }
            else _useCase.Handler(new() { UserName = "tester", Password = "password" }).Should().BeNull();
            A.CallTo(() => _tokenService.GenerateAccessToken(A<IEnumerable<Claim>>._)).MustNotHaveHappened();
            A.CallTo(() => _refreshTokenRepository.Create(A<ei_back.Core.Domain.Entity.RefreshToken>._)).MustNotHaveHappened();
        }
        [Fact]
        public void Handler_WhenUserNotFound_ReturnsNull()
        {
            var request = new LoginDtoRequest { UserName = "unknown", Password = "pass123" };

            A.CallTo(() => _userRepository.FindByUserName("unknown", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(null));

            var result = _useCase.Handler(request);

            result.Should().BeNull();
        }

        [Fact]
        public void Handler_WhenBcryptPasswordIsValid_ReturnsTokenResponse()
        {
            var request = new LoginDtoRequest { UserName = "testuser", Password = "pass123" };
            var user = new UserEntity("testuser", "Test User", "test@test.com", "$2a$11$abcdefghijklmnopqrstuv", UserRole.CommonUser);
            user.EmailVerifiedAt = DateTimeOffset.UtcNow;
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty("Id")!.SetValue(user, Guid.NewGuid());

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _encryptionService.VerifyBcryptHash("pass123", user.Password))
                .Returns(true);
            A.CallTo(() => _tokenService.GenerateAccessToken(A<IEnumerable<Claim>>._))
                .Returns("access-token");
            A.CallTo(() => _tokenService.GenerateRefreshToken())
                .Returns("refresh-token");
            A.CallTo(() => _encryptionService.ComputeSha256Hash("refresh-token"))
                .Returns("refresh-hash");

            var result = _useCase.Handler(request);

            result.Should().NotBeNull();
            result.Authenticated.Should().BeTrue();
            result.AccessToken.Should().Be("access-token");
            result.RefreshToken.Should().Be("refresh-token");
        }

        [Fact]
        public void Handler_WhenSha256PasswordIsValid_RehashesAndReturnsTokenResponse()
        {
            var request = new LoginDtoRequest { UserName = "testuser", Password = "pass123" };
            var user = new UserEntity("testuser", "Test User", "test@test.com", "OLD-SHA256-HASH", UserRole.CommonUser);
            user.EmailVerifiedAt = DateTimeOffset.UtcNow;
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty("Id")!.SetValue(user, Guid.NewGuid());

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _encryptionService.ComputeSha256("pass123"))
                .Returns("OLD-SHA256-HASH");
            A.CallTo(() => _encryptionService.ComputeBcryptHash("pass123"))
                .Returns("$2a$11$newbcrypthash");
            A.CallTo(() => _tokenService.GenerateAccessToken(A<IEnumerable<Claim>>._))
                .Returns("access-token");
            A.CallTo(() => _tokenService.GenerateRefreshToken())
                .Returns("refresh-token");
            A.CallTo(() => _encryptionService.ComputeSha256Hash("refresh-token"))
                .Returns("refresh-hash");

            var result = _useCase.Handler(request);

            result.Should().NotBeNull();
            result.Authenticated.Should().BeTrue();
            A.CallTo(() => _userRepository.RefreshUserInfo(user)).MustHaveHappened();
        }

        [Fact]
        public void Handler_WhenPasswordIsInvalid_ReturnsNull()
        {
            var request = new LoginDtoRequest { UserName = "testuser", Password = "wrong" };
            var user = new UserEntity("testuser", "Test User", "test@test.com", "$2a$11$somehash", UserRole.CommonUser);
            user.EmailVerifiedAt = DateTimeOffset.UtcNow;

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _encryptionService.VerifyBcryptHash("wrong", user.Password))
                .Returns(false);

            var result = _useCase.Handler(request);

            result.Should().BeNull();
        }
    }
}
