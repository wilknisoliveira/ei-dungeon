using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Role.Dtos;
using ei_back.Core.Application.UseCase.Role.Interfaces;
using ei_back.Core.Domain.Enums;
using ei_back.Infrastructure.Extensions;

namespace ei_back.Core.Application.UseCase.Role
{
    public class GetAllRoleUseCase : IGetAllRoleUseCase
    {
        private readonly IUserRepository _userRepository;
        public GetAllRoleUseCase(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<List<RoleDtoResponse>> Handler()
        {
            List<UserRole> roles = new();

            foreach (Enum role in Enum.GetValues(typeof(UserRole)))
            {
                roles.Add((UserRole)role);
            }

            Dictionary<UserRole, List<string>> usersByRole = await _userRepository.GetUsersNameGroupByRole();

            var rolesResponse = roles.Select(role =>
            {
                var roleDto = new RoleDtoResponse()
                {
                    Name = role.GetEnumDescription(),
                    Users = usersByRole.Where(x => x.Key.Equals((UserRole)role))?.Select(x => x.Value).FirstOrDefault() ?? [],
                };

                return roleDto;
            }).ToList();

            return rolesResponse;
        }
    }
}
