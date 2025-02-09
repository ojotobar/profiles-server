using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Dto
{
    public class ProfessionalDto
    {
        public Guid Id { get; set; } 
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public EGender Gender { get; set; }
        public EStatus Status { get; set; }
        public string? OtherName { get; set; }
        public DateTime LastLogin { get; set; }
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
        public DateTime DeactivatedOn { get; set; } = DateTime.MaxValue;
        public bool IsDeprecated { get; set; }
        public ProfessionalLocation? Location { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public List<string> Skills { get; set; } = [];
        public string? PhotoUrl { get; set; }
        public string? CVUrl { get; set; }

        public static ProfessionalDto? MapData(Professional? user)
        {
            return user == null ? null : new ProfessionalDto
            {
                Id = user.Id,
                FirstName = user.FirstName!,
                LastName = user.LastName,
                OtherName = user.OtherName,
                Gender = user.Gender,
                Status = user.Status,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                DeactivatedOn = user.DeactivatedOn,
                IsDeprecated = user.IsDeprecated,
                LastLogin = user.LastLogin,
                UpdatedOn = user.UpdatedOn,
                Location = user.Location,
                Skills = user.Skills,
                CVUrl = user.ResumeLink,
                PhotoUrl = user.ProfilePicture
            };
        }
    }
}
