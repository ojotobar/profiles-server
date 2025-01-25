namespace ProfessionalProfiles.GraphQL.Account
{
    public class ChangePasswordInput
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
