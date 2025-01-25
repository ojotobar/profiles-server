namespace ProfessionalProfiles.GraphQL.Account
{
    public class LoginInput : EmailInput
    {
        public string Password { get; set; } = string.Empty;
    }
}
