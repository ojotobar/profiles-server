using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Educations
{
    public record EducationResult(Education? Education, string Message, bool Success = false);
}
