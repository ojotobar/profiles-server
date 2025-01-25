using Mongo.Common;
using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.Entities.Models
{
    public class Education : IBaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public string InstitutionName { get; set; } = string.Empty;
        [Required]
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public Location? Location { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
        public bool IsDeprecated { get; set; }
    }
}
