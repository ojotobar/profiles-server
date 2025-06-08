using System.ComponentModel;

namespace ProfessionalProfiles.Entities.Enums
{
    public enum ESkillLevel
    {
        [Description("(Beginner) – Soft neutral.")]
        Beginner,
        [Description("(Novice) – Cautious, emerging.")]
        Novice,
        [Description("(Intermediate) – Stable, learning.")]
        Intermediate,
        [Description("(Advanced) – Confident, solid.")]
        Advanced,
        [Description("(Expert) – Mastery, wisdom.")]
        Expert
    }
}
