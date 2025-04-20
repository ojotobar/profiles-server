using System.ComponentModel;

namespace ProfessionalProfiles.Entities.Enums
{
    public enum EAction
    {
        [Description("Logged In")]
        LoggedIn,
        [Description("Verified Account")]
        VerifiedAccount,
        [Description("Updated Role for {0}")]
        RoleUpdate,
        [Description("Updated Profile: {0}")]
        ProfileUpdate,
        [Description("Changed Status for {0}")]
        StatusChange,
        [Description("Changed Password")]
        PasswordChange,
        [Description("Reset Password")]
        PasswordReset,
        [Description("Changed Forgotten Password")]
        ForgottenPasswordChange,
        [Description("Deprecated accouount for {0}")]
        Deprecated,
        [Description("Deleted for {0}")]
        Deleted,
        [Description("Deactivated Account")]
        Deactivated,
        [Description("{0} audit log with title {1}")]
        AuditLogs,
        [Description("{0} role: {1}")]
        RoleAdmin
    }
}
