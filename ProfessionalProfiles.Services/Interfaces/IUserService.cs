using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Shared.DTOs;

namespace ProfessionalProfiles.Services.Interfaces
{
    public interface IUserService
    {
        Task<AccessTokenDto> CreateAccessToken(AccessTokenDto tokenDto, Professional user);
        Task<(bool Valid, string Message)> IsValidApiKey(string base64String);
        Task<AccessTokenDto> Validate(string email, string password);
    }
}
