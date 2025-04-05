using ProfessionalProfiles.Entities.Enums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ProfessionalProfiles.Graph.Account
{
    public record RegisterUserInput
    {
        [Required]
        public string FirstName { get; set; } = string.Empty;
        [Required]
        public string LastName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        [Required, EmailAddress]
        public string EmailAddress { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;
        [Compare("Password", ErrorMessage = "Password and Compare Password fields must match")]
        public string ConfirmPassword { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public EGender Gender { get; set; } = EGender.NotSpecified;
        public ERoles? Role { get; set; }
        [JsonIgnore]
        public bool MatchPassword => !string.IsNullOrWhiteSpace(Password) &&
            !string.IsNullOrWhiteSpace(ConfirmPassword) && Password.Equals(ConfirmPassword);
    }

    public record ProfileDetailsInput(string FirstName, string LastName, string? OtherName, string Phone, EGender Gender);
}
