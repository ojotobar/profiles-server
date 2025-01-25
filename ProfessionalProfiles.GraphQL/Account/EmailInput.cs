using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.GraphQL.Account
{
    public abstract class EmailInput
    {
        [EmailAddress, Required]
        public string Email { get; set; } = string.Empty;
    }
}
