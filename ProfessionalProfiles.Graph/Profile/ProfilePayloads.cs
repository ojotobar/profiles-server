namespace ProfessionalProfiles.Graph.Profile
{
    public record UploadResult(Guid UserId, string Link, string Message, bool Success = false);
}
