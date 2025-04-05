namespace ProfessionalProfiles.Graph.Account
{
    public record ApiKeyPayload(string ApiKey, string Message, bool Success = false);
}