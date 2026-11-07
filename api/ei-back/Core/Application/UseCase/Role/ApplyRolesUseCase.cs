using AutoMapper;
using ei_back.Core.Application.Repository;
using ei_back.Core.Application.UseCase.Role.Dtos;
using ei_back.Core.Application.UseCase.Role.Interfaces;

namespace ei_back.Core.Application.UseCase.Role
{
    public class ApplyRolesUseCase : IApplyRolesUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;

        public ApplyRolesUseCase(IUserRepository userRepository, IMapper mapper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
        }

        public async Task<ApplyRoleDtoResponse> Handler(ApplyRoleDtoRequest applyRoleDtoRequest)
        {
            var user = await _userRepository.GetUserAndRolesAsync(applyRoleDtoRequest.Id);

            user.Role = applyRoleDtoRequest.role;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var userResponse = _userRepository.Update(user);

            var applyRoleDtoResponse = _mapper.Map<ApplyRoleDtoResponse>(userResponse);

            return applyRoleDtoResponse;
        }
    }
}
