namespace ei_back.Core.Application.UseCase.User.Dtos
{
    public class UserGetDtoResponse
    {
        public Guid Id { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
    }
}
