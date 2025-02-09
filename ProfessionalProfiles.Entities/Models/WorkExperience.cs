using Mongo.Common;
using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.Entities.Models
{
    public class WorkExperience : IBaseEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
        public bool IsDeprecated { get; set; }
        public Guid UserId { get; set; }
        [Required]
        public string Organization { get; set; } = string.Empty;
        [Required]
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        [Required]
        public string JobTitle { get; set; } = string.Empty;
        [Required]
        public List<string> Accomplishments { get; set; } = [];
        public EntityLocation Location { get; set; } = new();
    }
}
