﻿using ProfessionalProfiles.Entities.Enums;
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
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; } = DateTime.Now;
        public DateTime DeactivatedOn { get; set; } = DateTime.MaxValue;
        public bool IsDeprecated { get; set; }
        public ProfessionalLocation? Location { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
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
                CreatedOn = user.CreatedOn,
                DeactivatedOn = user.DeactivatedOn,
                IsDeprecated = user.IsDeprecated,
                LastLogin = user.LastLogin,
                UpdatedOn = user.UpdatedOn,
                Location = user.Location,
                CVUrl = user.ResumeLink,
                PhotoUrl = user.ProfilePicture
            };
        }
    }

    public record ProfileSummaryDto(long Education, long Experience, long Skills, long Projects, long Certifications, 
        bool HasCareerSummary, int Progress, bool CanGenerateApiKey, string? ApiKey = "");
}
