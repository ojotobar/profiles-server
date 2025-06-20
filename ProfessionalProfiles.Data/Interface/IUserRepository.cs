using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IUserRepository
    {
        Task<Guid> GetLoggedInOrApiKeyUserId(ApiAccessInput? apiAccessInput = null);
        string GetLoggedInUserId();
        Task<List<ERoles>> GetUserRoles();
        Task<bool> HasEqualOrHigherRole(ERoles role);
    }
}
