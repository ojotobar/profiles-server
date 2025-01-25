using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.Entities.Models
{
    public class EntityLocation
    {
        [Required]
        public string City { get; set; } = string.Empty;
        [Required]
        public string State { get; set; } = string.Empty;
        [Required]
        public string Country { get; set; } = string.Empty;
        public string? Longitude { get; set; } = string.Empty;
        public string? Latitude { get; set; } = string.Empty;
    }
}
