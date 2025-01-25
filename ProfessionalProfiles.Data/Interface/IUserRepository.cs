using ProfessionalProfiles.Entities.Enums;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IUserRepository
    {
        string GetLoggedInUserId();
        Task<List<ERoles>> GetUserRoles();
        Task<bool> HasEqualOrHigherRole(ERoles role);
    }
}
