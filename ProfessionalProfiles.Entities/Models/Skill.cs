using CSharpTypes.Extensions.Enumeration;
using Mongo.Common;
using ProfessionalProfiles.Entities.Enums;

namespace ProfessionalProfiles.Entities.Models
{
    public class Skill : IBaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public bool Certified { get; set; }
        public ESkillLevel Level { get; set; }
        public string LevelDescription => Level.GetDescription();
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
        public bool IsDeprecated { get; set; }
    }
}
