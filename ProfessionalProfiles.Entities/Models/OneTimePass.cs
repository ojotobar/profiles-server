using Mongo.Common;
using ProfessionalProfiles.Entities.Enums;
using System.ComponentModel.DataAnnotations;

namespace ProfessionalProfiles.Entities.Models
{
    public class OneTimePass : IBaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        [Required]
        public string Otp { get; set; } = string.Empty;
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public DateTime ExpiresOn { get; set; }
        [Required]
        public EOtpType PassType { get; set; }
        public bool Used { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime CreatedOn { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedOn { get; set; } = DateTime.UtcNow;
        public bool IsDeprecated { get; set; }
    }
}
