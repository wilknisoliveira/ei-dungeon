using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Role;
using ei_back.Core.Application.UseCase.Role.Dtos;
using ei_back.Core.Application.UseCase.Role.Interfaces;
using ei_back.Core.Domain.Enums;
using UserEntity = ei_back.Core.Domain.Entity.User;

namespace ei_back.Tests.Core.Application.UseCase.Role
{
    public class ApplyRolesUseCaseTests
    {
        [Fact]
        public async Task Handler_UpdatesUserRoleAndReturnsResponse()
        {
            var userRepository = A.Fake<IUserRepository>();
            var mapper = A.Fake<IMapper>();
            var useCase = new ApplyRolesUseCase(userRepository, mapper);

            var userId = Guid.NewGuid();
            var request = new ApplyRoleDtoRequest { Id = userId, role = UserRole.PremiumUser };
            var user = new UserEntity("testuser", "Test", "t@t.com", "hash", UserRole.CommonUser);
            typeof(ei_back.Core.Domain.Entity.Base).GetProperty("Id")!.SetValue(user, userId);
            var response = new ApplyRoleDtoResponse { Id = userId, UserName = "testuser", role = UserRole.PremiumUser };

            A.CallTo(() => userRepository.GetUserAndRolesAsync(userId, CancellationToken.None))
                .Returns(Task.FromResult<UserEntity?>(user));
            A.CallTo(() => userRepository.Update(user))
                .Returns(user);
            A.CallTo(() => mapper.Map<ApplyRoleDtoResponse>(user))
                .Returns(response);

            var result = await useCase.Handler(request);

            result.Should().Be(response);
            user.Role.Should().Be(UserRole.PremiumUser);
        }
    }
}
