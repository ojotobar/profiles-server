using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.Entities.Models
{
    public class ProfessionalLocation : EntityLocation
    {
        [Required]
        public string Line1 { get; set; } = string.Empty;
        public string? Line2 { get; set; }
        [Required]
        public string PostalCode { get; set; } = string.Empty;
    }
}