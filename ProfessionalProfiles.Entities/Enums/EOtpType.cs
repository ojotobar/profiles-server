using System.ComponentModel;

namespace ProfessionalProfiles.Entities.Enums
{
    public enum EOtpType
    {
        [Description("Account verifiction")]
        Verification = 1,
        [Description("Password reset")]
        PasswordReset
    }
}
