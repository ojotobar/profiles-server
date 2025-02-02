using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using System.Net;

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

        // Default props
        public bool IsSuccessful { get; set; }
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;
        public string? Message { get; set; }

        public static ProfessionalDto MapData(Professional? user, HttpStatusCode code, string? message = "", bool successful = false)
        {
            return user == null ? new ProfessionalDto() : new ProfessionalDto
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
                IsSuccessful = successful,
                StatusCode = code,
                Message = message
            };
        }
    }
}
