using System.ComponentModel;

namespace ProfessionalProfiles.Entities.Enums
{
    public enum EEducationLevel
    {
        [Description("{0}")]
        Other,
        [Description("OND")]
        OrdinaryDiploma,
        [Description("HND")]
        HigherDiploma,
        [Description("Bachelors")]
        Bachelor,
        [Description("Masters")]
        Masters,
        [Description("Ph.D")]
        Doctorate
    }
}
