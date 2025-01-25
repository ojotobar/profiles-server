using System.ComponentModel;

namespace ProfessionalProfiles.Entities.Enums
{
    public enum EEducationLevel
    {
        [Description("Others")]
        Other,
        [Description("OND")]
        OrdinaryDiploma,
        [Description("HND")]
        HigherDiploma,
        [Description("Bachelor")]
        Bachelor,
        [Description("Master")]
        Masters,
        [Description("Ph.D")]
        Doctorate
    }
}
