using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Projects
{
    public record ProjectInput(string ProjectName, string? Link, string Summary, List<string>? Technologies);
    public record ProjectPayload(Project? Project, string Message, bool Success = false);
    public record ProjectsPayload(List<Project> Projects, string Message, bool Success = false);
}
