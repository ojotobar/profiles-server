using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.Graph.Account
{
    public abstract class EmailInput
    {
        [EmailAddress, Required]
        public string Email { get; set; } = string.Empty;
    }
}
