using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.String;
using HotChocolate;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.GraphQL.Account;
using ProfessionalProfiles.GraphQL.Dto;
using ProfessionalProfiles.GraphQL.Profile;
using ProfessionalProfiles.Shared.Extensions;
using System.Net;

namespace ProfessionalProfiles.GraphQL
{
    public class Query
    {
        /// <summary>
        /// Generates Api Key for Users' Endpoint access
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<ApiKeyPayload> GetApiKeyAsync([Service] IRepositoryManager repository, 
            [Service] UserManager<Professional> userManager)
        {
            var loggedInUserId = repository.User.GetLoggedInUserId();
            if (loggedInUserId.IsNullOrEmpty())
            {
                return new ApiKeyPayload(ApiKeyDto.Initialize("", "Access denied", HttpStatusCode.Unauthorized));
            }

            var user = await userManager.FindByIdAsync(loggedInUserId);
            if (user == null)
            {
                return new ApiKeyPayload(ApiKeyDto.Initialize("", "User not found", HttpStatusCode.NotFound));
            }

            var ticks = DateTime.UtcNow.Ticks;
            user!.KeyMarker = ticks;

            var apiKey = user.Id.EncodeGuidAsBase64(ticks);
            await userManager.UpdateAsync(user);
            return new ApiKeyPayload(ApiKeyDto.Initialize(apiKey, "", HttpStatusCode.OK, true));
        }


        [Authorize]
        public async Task<ProfilePayload> GetProfileAsync([Service] UserManager<Professional> userManager,
            [Service] IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInUserId();
            if (userId.IsNullOrEmpty())
            {
                return new ProfilePayload(ProfessionalDto.MapData(null, HttpStatusCode.Unauthorized, "Access denied!"));
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new ProfilePayload(ProfessionalDto.MapData(user, HttpStatusCode.NotFound, "User not found!"));
            }   

            return new ProfilePayload(ProfessionalDto.MapData(user, HttpStatusCode.OK, successful: true));
        }
    }
}
