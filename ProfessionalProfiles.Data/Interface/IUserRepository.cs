using ProfessionalProfiles.Entities.Enums;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IUserRepository
    {
        Guid GetLoggedInOrApiKeyUserId(string apiKey);
        string GetLoggedInUserId();
        Task<List<ERoles>> GetUserRoles();
        Task<bool> HasEqualOrHigherRole(ERoles role);
    }
}
