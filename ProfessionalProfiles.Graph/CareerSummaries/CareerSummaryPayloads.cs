using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.CareerSummaries
{
    public record ProfessionalSummaryInput(string Summary);

    public record ProfessionalSummaryPayload(ProfessionalSummary? ProfessionalSummary, string Message, bool Success = false);
}
