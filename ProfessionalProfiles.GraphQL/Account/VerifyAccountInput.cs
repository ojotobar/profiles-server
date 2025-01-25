using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.GraphQL.Account
{
    public class VerifyAccountInput : EmailInput
    {
        [Required]
        public string OTP { get; set; } = string.Empty;
    }
}
