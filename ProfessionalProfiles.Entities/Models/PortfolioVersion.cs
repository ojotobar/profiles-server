using Mongo.Common;
using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.Entities.Models
{
    public class PortfolioVersion : IBaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Name { get; set; } = string.Empty;
        [Required]
        public string LatestVersion { get; set; } = string.Empty;
        public string? OldVersion { get; set; }
        public bool IsPremium { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
        public bool IsDeprecated { get; set; }
    }
}
