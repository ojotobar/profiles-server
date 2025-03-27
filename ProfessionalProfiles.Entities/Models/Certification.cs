using CSharpTypes.Extensions.Object;
using Mongo.Common;

namespace ProfessionalProfiles.Entities.Models
{
    public class Certification : IBaseEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Institution { get; set; } = string.Empty;
        public string? Link { get; set; }
        public DateTime DateObtained { get; set; }
        public int? YearsOfValidity => Expires.HasValue ? 
            Expires.Value.Year - DateObtained.Year : 
            null;
        public DateTime? Expires { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
        public bool IsDeprecated { get; set; }
    }
}
