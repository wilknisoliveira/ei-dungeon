using System.ComponentModel;

namespace ei_back.Core.Domain.Enums
{
    public enum UserRole : short
    {
        [Description("Admin")] Admin = 0,
        [Description("CommonUser")] CommonUser = 1,
        [Description("PremiumUser")] PremiumUser = 2,
    }
}
