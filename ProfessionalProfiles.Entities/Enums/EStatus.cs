using System.ComponentModel;

namespace ProfessionalProfiles.Entities.Enums
{
    public enum EStatus
    {
        [Description("Inactive")]
        Inactive,
        [Description("Active")]
        Active,
        [Description("Suspended")]
        Suspended,
        [Description("Delete")]
        Deleted
    }
}
