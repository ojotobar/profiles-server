namespace ProfessionalProfiles.Graph.Account
{
    public record LoginResult(string AccessToken, string UserName, string Message, bool Successful = false);
    public record AccountResult(string Email, string Message, bool Successful = false);
}
