using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.Graph.Account
{
    public class ForgotPasswordInput : EmailInput
    {
        [Required]
        public string Code { get; set; } = string.Empty;
        [Required(ErrorMessage = "New Password is required.")]
        public string NewPassword { get; set; } = string.Empty;
        [Required(ErrorMessage = "Confirm New Password is required.")]
        [Compare(nameof(NewPassword), ErrorMessage = "New Password and Confirm New Password must match.")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
