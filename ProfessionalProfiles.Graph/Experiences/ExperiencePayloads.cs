using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Experiences
{
    public record LocationInput(string City, string Country);
    public record ExperienceInput(string Organization, string Title, DateTime StartDate, DateTime? EndDate, List<string>? Summaries, LocationInput Location);
    public record ExperiencePayload(WorkExperience? Experience, string Message, bool Success = false);
    public record ExperiencesPayload(List<WorkExperience> Experiences, string Message, bool Success = false);
}
