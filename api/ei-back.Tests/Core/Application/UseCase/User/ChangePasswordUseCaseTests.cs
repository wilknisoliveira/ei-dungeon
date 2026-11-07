using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.Service.Encryption.Interfaces;
using ei_back.Core.Application.UseCase.User;
using ei_back.Core.Application.UseCase.User.Dtos;
using ei_back.Core.Application.UseCase.User.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Exceptions.ExceptionTypes;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.User
{
    public class ChangePasswordUseCaseTests
    {
        private readonly IUserRepository _userRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly IMapper _mapper;
        private readonly IChangePasswordUseCase _useCase;

        public ChangePasswordUseCaseTests()
        {
            _userRepository = A.Fake<IUserRepository>();
            _encryptionService = A.Fake<IEncryptionService>();
            _mapper = A.Fake<IMapper>();
            _useCase = new ChangePasswordUseCase(_userRepository, _encryptionService, _mapper);
        }

        [Fact]
        public async Task Handler_WhenUserNotFound_ThrowsNotFoundException()
        {
            var request = new PasswordDtoRequest { CurrentPassword = "old", NewPassword = "new" };

            A.CallTo(() => _userRepository.FindByUserName("unknown", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(null));

            Func<Task> act = () => _useCase.Handler("unknown", request);

            await act.Should().ThrowAsync<NotFoundException>().WithMessage("*unknown*");
        }

        [Fact]
        public async Task Handler_WhenCurrentPasswordIsWrong_ThrowsBadRequestException()
        {
            var request = new PasswordDtoRequest { CurrentPassword = "wrong", NewPassword = "new" };
            var user = new UserEntity("testuser", "Test", "t@t.com", "$2a$11$somehash", UserRole.CommonUser);

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _encryptionService.VerifyBcryptHash("wrong", user.Password))
                .Returns(false);

            Func<Task> act = () => _useCase.Handler("testuser", request);

            await act.Should().ThrowAsync<BadRequestException>().WithMessage("Wrong password!");
        }

        [Fact]
        public async Task Handler_WhenBcryptPasswordValid_UpdatesAndReturnsUser()
        {
            var request = new PasswordDtoRequest { CurrentPassword = "old", NewPassword = "new" };
            var user = new UserEntity("testuser", "Test", "t@t.com", "$2a$11$somehash", UserRole.CommonUser);
            var response = new UserGetDtoResponse { Id = Guid.NewGuid(), UserName = "testuser", FullName = "Test", Email = "t@t.com" };

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _encryptionService.VerifyBcryptHash("old", user.Password))
                .Returns(true);
            A.CallTo(() => _encryptionService.ComputeBcryptHash("new"))
                .Returns("$2a$11$newhash");
            A.CallTo(() => _userRepository.Update(user))
                .Returns(user);
            A.CallTo(() => _mapper.Map<UserGetDtoResponse>(user))
                .Returns(response);

            var result = await _useCase.Handler("testuser", request);

            result.Should().Be(response);
            user.Password.Should().Be("$2a$11$newhash");
        }

        [Fact]
        public async Task Handler_WhenSha256PasswordValid_UpdatesAndReturnsUser()
        {
            var request = new PasswordDtoRequest { CurrentPassword = "old", NewPassword = "new" };
            var user = new UserEntity("testuser", "Test", "t@t.com", "OLD-SHA256-HASH", UserRole.CommonUser);
            var response = new UserGetDtoResponse { Id = Guid.NewGuid(), UserName = "testuser", FullName = "Test", Email = "t@t.com" };

            A.CallTo(() => _userRepository.FindByUserName("testuser", CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => _encryptionService.ComputeSha256("old"))
                .Returns("OLD-SHA256-HASH");
            A.CallTo(() => _encryptionService.ComputeBcryptHash("new"))
                .Returns("$2a$11$newhash");
            A.CallTo(() => _userRepository.Update(user))
                .Returns(user);
            A.CallTo(() => _mapper.Map<UserGetDtoResponse>(user))
                .Returns(response);

            var result = await _useCase.Handler("testuser", request);

            result.Should().Be(response);
            user.Password.Should().Be("$2a$11$newhash");
        }
    }
}
