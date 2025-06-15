using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Dto;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class AccountQueries
    {
        /// <summary>
        /// Get list of system roles
        /// </summary>
        /// <param name="roleManager"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public IQueryable<AppRoleDto> GetSystemRoles([Service] RoleManager<AppRole> roleManager,
            [Service] UserManager<Professional> userManager)
        {
            return roleManager.Roles
                .Map(userManager)
                .OrderBy(r => r.Name);
        }
    }
}
