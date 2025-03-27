using System.ComponentModel;

namespace ProfessionalProfiles.Entities.Enums
{
    public enum ESkillLevel
    {
        [Description("(Basic) – Limited practical experience.")]
        FundamentalAwareness,
        [Description("(Limited Experience) – Can perform simple tasks with help.")]
        Novice,
        [Description("(Working Experience) – Can work independently.")]
        Intermediate,
        [Description("(Proficient) – Strong expertise in the skill.")]
        Advanced,
        [Description("(Mastery) – Recognized authority in the field.")]
        Expert
    }
}
