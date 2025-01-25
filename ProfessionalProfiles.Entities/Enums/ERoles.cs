using System.ComponentModel;

namespace ProfessionalProfiles.Entities.Enums
{
    public enum ERoles
    {
        [Description("Professional")]
        Professional = 1,
        [Description("ReadOnly")]
        ReadOnly,
        [Description("Admin")]
        Admin
    }
}
