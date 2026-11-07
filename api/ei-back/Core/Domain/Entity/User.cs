using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Domain.Entity
{
    public class User : Base
    {
        public User()
        {
        }

        public User(string userName, string fullName, string email, string password, UserRole role)
        {
            UserName = userName;
            FullName = fullName;
            Email = email;
            Password = password;
            Role = role;
        }

        public string UserName { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string NormalizedEmail { get; set; } = string.Empty;
        public DateTimeOffset? EmailVerifiedAt { get; set; }
        public string Password { get; set; }
        public UserRole Role { get; set; }

        public List<Game> Games { get; set; } = new();
    }
}
