using System.ComponentModel;

namespace ProfessionalProfiles.Entities.Enums
{
    public enum EAction
    {
        [Description("Logged In")]
        LoggedIn,
        [Description("Verified Account")]
        VerifiedAccount,
        [Description("Updated Role from {0} to {1}")]
        RoleUpdate,
        [Description("Updated Profile: {0}")]
        ProfileUpdate,
        [Description("Status changed from {0} to {1}")]
        StatusChange,
        [Description("Password Change")]
        PasswordChange,
        [Description("Password Reset")]
        PasswordReset,
        [Description("Change Forgotten Password")]
        ForgottenPasswordChange,
        [Description("Deprecated accouount for {0}")]
        Deprecated,
        [Description("Deleted {0}")]
        Deleted
    }
}
