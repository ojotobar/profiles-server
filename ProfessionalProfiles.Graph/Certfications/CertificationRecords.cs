using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Certfications
{
    public record CertificationInput(string Name, string InstitutionName, DateTime Date, int? YearsOfValidity, string? Link);
    public record CertificationPayload(Certification? Certfication, string Message, bool Success = false);
    public record CertificationsPayload(List<Certification> Certfications, string Message, bool Success = false);
}