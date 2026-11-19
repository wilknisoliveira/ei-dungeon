using ei_back.Core.Domain.Enums;

namespace ei_back.Core.Domain.Entity
{
    public class User : Base
    {
        public User()
        {
        }

        public User(string email, string? password, UserRole role)
        {
            Email = email;
            Password = password;
            Role = role;
        }

        public string Email { get; set; } = string.Empty;
        public string NormalizedEmail { get; set; } = string.Empty;
        public DateTimeOffset? EmailVerifiedAt { get; set; }
        public string? Password { get; set; }
        public UserRole Role { get; set; }

        public List<Game> Games { get; set; } = new();
    }
}
