using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Application.UseCase.Role.Dtos
{
    public class ApplyRoleDtoResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public UserRole role { get; set; }
    }
}
