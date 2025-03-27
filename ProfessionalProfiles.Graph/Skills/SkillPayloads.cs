using ProfessionalProfiles.Entities.Enums;

namespace ProfessionalProfiles.Graph.Skills
{
    public record SkillInput(string Name, ESkillLevel Level, int Years, bool IsCertified = false);
}
