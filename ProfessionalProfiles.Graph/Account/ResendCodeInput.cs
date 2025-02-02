using ProfessionalProfiles.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.Graph.Account
{
    public class ResendCodeInput : EmailInput
    {
        [Required]
        public EOtpType CodeType { get; set; }
    }
}
