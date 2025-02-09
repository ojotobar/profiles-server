using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Skills
{
    public record SkillInput(string Name);
    public record SkillPayload(Skill? Skill, string Message, bool Success = false);
    public record SkillsPayload(List<Skill> Skills, string Message, bool Success = false);
}
