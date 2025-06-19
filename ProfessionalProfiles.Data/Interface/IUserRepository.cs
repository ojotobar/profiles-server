using ProfessionalProfiles.Entities.Enums;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IUserRepository
    {
        Task<Guid> GetLoggedInOrApiKeyUserId(string apiKey, string appTag = "");
        string GetLoggedInUserId();
        Task<List<ERoles>> GetUserRoles();
        Task<bool> HasEqualOrHigherRole(ERoles role);
    }
}
