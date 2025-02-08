using CSharpTypes.Extensions.Enumeration;
using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.String;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Shared.Extensions;
using System.Security.Claims;

namespace ProfessionalProfiles.Data.Implementations
{
    public class UserRepository(IHttpContextAccessor contextAccessor,
        UserManager<Professional> userManager) : IUserRepository
    {
        private readonly IHttpContextAccessor contextAccessor = contextAccessor;
        private readonly UserManager<Professional> userManager = userManager;

        /// <summary>
        /// Gets logged in userId
        /// </summary>
        /// <returns></returns>
        public string GetLoggedInUserId()
        {
            return GetUserId();
        }

        public Guid GetLoggedInOrApiKeyUserId(string apiKey)
        {
            var userId = GetLoggedInUserId().ToGuid();
            userId = userId.IsEmpty() ?
                StringTypeExtensions.DecodeBase64StringAsGuid(apiKey ?? string.Empty) :
                userId;

            return userId;
        }

        /// <summary>
        /// Gets logged in user roles
        /// </summary>
        /// <returns></returns>
        public async Task<List<ERoles>> GetUserRoles()
        {
            var roles = new List<ERoles>();
            var userId = GetUserId();
            if (userId.IsNotNullOrEmpty())
            {
                var user = await userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return roles;
                }

                return (await userManager.GetRolesAsync(user)).ToList().ParseValues<ERoles>();
            }

            return roles;
        }

        /// <summary>
        /// Checks if the logged in user has a role with equal or higher rights than the specified role
        /// </summary>
        /// <param name="role">The role against which we are checking the user rights</param>
        /// <returns></returns>
        public async Task<bool> HasEqualOrHigherRole(ERoles role)
        {
            var roles = await GetUserRoles();
            return roles.Any(x => (int)x >= (int)role);
        }

        private string GetUserId()
        {
            ClaimsPrincipal? userClaim = contextAccessor.HttpContext?.User;
            return userClaim?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        }
    }
}
