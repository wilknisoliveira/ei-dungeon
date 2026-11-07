namespace ei_back.Core.Application.UseCase.User.Dtos
{
    public class UserInfoCheckResponseDto
    {
        public bool UsernameAvailable { get; set; }
        public bool EmailAvailable { get; set; }
    }
}
