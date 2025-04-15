using Mongo.Common;
using ProfessionalProfiles.Entities.Enums;

namespace ProfessionalProfiles.Entities.Models
{
    public class AuditLog : IBaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string UserId { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public string? PerformedOn { get; set; }
        public EAction ActionId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string IPAddress { get; set; } = string.Empty;
        public string Platform { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
        public bool IsDeprecated { get; set; }
    }
}
