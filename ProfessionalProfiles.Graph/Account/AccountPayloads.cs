namespace ProfessionalProfiles.Graph.Account
{
    public record LoginResult(string AccessToken, string UserName, string Message, bool Successful = false, bool EmailNotConfirmed = false);
}
