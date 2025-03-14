using AspNetCore.Identity.MongoDbCore.Models;
using ProfessionalProfiles.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.Entities.Models
{
    public class Professional : MongoIdentityUser<Guid>
    {
        [Required] 
        public string FirstName { get; set; } = string.Empty;
        [Required] 
        public string LastName { get; set; } = string.Empty;
        public string? OtherName { get; set; }
        [Required]
        public EGender Gender { get; set; }
        public DateTime LastLogin { get; set; }
        public EStatus Status { get; set; } = EStatus.Inactive;
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
        public DateTime DeactivatedOn { get; set; } = DateTime.MaxValue;
        public bool IsDeprecated { get; set; }
        public bool IsPremium { get; set; }
        public ProfessionalLocation? Location { get; set; }
        public long KeyMarker { get; set; }
        public string? ProfilePicture { get; set; }
        public string? ResumeLink { get; set; }
        public List<string> Skills { get; set; } = [];
    }
}
