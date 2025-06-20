using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.String;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Account;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.Extensions;
using ProfessionalProfiles.Shared.Extensions;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class ProfileQueries
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
                return new ApiKeyPayload("", "Access denied");
            }

            var user = await userManager.FindByIdAsync(loggedInUserId);
            if (user == null)
            {
                return new ApiKeyPayload("", "User not found");
            }

            var canGenerate = await repository.CanGenerateApiKey(user.EmailConfirmed,
                user.Location != null, user.ProfilePicture != null, user.ResumeLink != null, user.SocialMedia.IsNotNullOrEmpty());

            if (canGenerate.CanGenerate)
            {
                var ticks = DateTime.UtcNow.Ticks;
                user!.KeyMarker = ticks;

                var apiKey = user.Id.EncodeGuidAsBase64(ticks);
                await userManager.UpdateAsync(user);
                return new ApiKeyPayload(apiKey, "Your API Key successfully generated.", true);
            }
            else
            {
                return new ApiKeyPayload("",
                    $"You are not allowed to generate API Key at the moment. Get your account completion to 80% from {canGenerate.Progress}% to continue");
            }
        }

        /// <summary>
        /// Gets User profile details
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public async Task<ProfessionalDto?> GetProfileAsync([Service] UserManager<Professional> userManager,
            [Service] IRepositoryManager repository, [GlobalState] string? apiKey = "", [GlobalState] string? clientTag = "")
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiKey!, clientTag!);

            if (userId.IsEmpty())
            {
                return null;
            }

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return null;
            }

            return ProfessionalDto.MapData(user);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ProfileSummaryResult> GetProfileSummariesAsync([Service] UserManager<Professional> userManager,
            [Service] IRepositoryManager repository, [GlobalState] string? apiKey = "", [GlobalState] string? clientTag = "")
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiKey!, clientTag!);

            if (userId.IsEmpty())
            {
                return new ProfileSummaryResult(false, null);
            }

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new ProfileSummaryResult(false, null);
            }

            var summary = await repository.GetProfileSummaryForMenus(user);

            return new ProfileSummaryResult(true, summary);
        }

        /// <summary>
        /// Gets user's social media records
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<SocialMediaResult> GetSocialMediaAsync([Service] UserManager<Professional> userManager,
            [Service] IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInUserId();
            if (!userId.IsNotNullOrEmpty())
            {
                return new SocialMediaResult(false, []);
            }

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new SocialMediaResult(false, []);
            }

            return new SocialMediaResult(true, user.SocialMedia.ToList());
        }

        /// <summary>
        /// Gets user summary data
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        public async Task<ProfileSummaryDto?> GetUserSummaryAsync([Service] IRepositoryManager repository,
            [Service] UserManager<Professional> userManager, [GlobalState] string? apiKey = "", [GlobalState] string? clientTag = "")
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiKey!, clientTag!);
            if (userId.IsEmpty())
            {
                return null;
            }

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return null;
            }

            return await repository.GetProfileSummary(user);
        }

        /// <summary>
        /// Gets detailed user profile records
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<DetailedProfileDto?> GetDetailedProfileAsync([Service] UserManager<Professional> userManager, 
            [Service] IRepositoryManager repository)
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId("");

            if (userId.IsEmpty())
            {
                return null;
            }

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return null;
            }

            var summary = await repository.GetProfileSummary(user);
            return new DetailedProfileDto(ProfessionalDto.MapData(user)!, summary);
        }

        /// <summary>
        /// Gets user's profile records
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ProfileDto?> GetProfileRecordAsync([Service] UserManager<Professional> userManager, 
            [Service] IRepositoryManager repository, [GlobalState] string? apiKey = "", [GlobalState] string? clientTag = "")
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiKey!, clientTag!);

            if (userId.IsEmpty())
            {
                return new ProfileDto();
            }

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new ProfileDto();
            }

            var summary = await repository.GetProfileSummary(user.Id);
            return new ProfileDto(ProfessionalDto.MapData(user)!, summary, true);
        }

        /// <summary>
        /// Gets the user's contact information
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ContactInfo?> GetUserContactInfoAsync([Service] UserManager<Professional> userManager,
            [Service] IRepositoryManager repository, [GlobalState] string? apiKey = "", [GlobalState] string? clientTag = "")
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiKey!, clientTag!);

            if (userId.IsEmpty())
            {
                return new ContactInfo();
            }

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new ContactInfo();
            }

            return new ContactInfo(user.Email ?? "", user.PhoneNumber ?? "", [.. user.SocialMedia], user.Location, true);
        }

        /// <summary>
        /// Gets a paginated list of users
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        [UseOffsetPaging(IncludeTotalCount = true)]
        [Authorize(Roles = ["Admin"])]
        public async Task<IQueryable<ProfessionalDto>> GetUsersAsync([Service] UserManager<Professional> userManager,
            UserFilterInput? search)
        {
            return (await userManager.Users
                .Filter(search)
                .MapAsync(userManager))
                .OrderByDescending(u => u.CreatedOn);
        }

        /// <summary>
        /// Get user's profile photo file object
        /// </summary>
        /// <param name="imageUrl"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<ProfileImageResult> GetProfileImageAsync([Service] UserManager<Professional> userManager,
            [Service] IRepositoryManager repository, [GlobalState] string? apiKey = "", [GlobalState] string? clientTag = "")
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiKey!, clientTag!);

            if (userId.IsEmpty())
            {
                throw new Exception("Invalid API Key");
            }

            var user = await userManager.FindByIdAsync(userId.ToString()) ?? throw new Exception("User not found");

            if (string.IsNullOrWhiteSpace(user.ProfilePicture))
                throw new ArgumentException("Image URL is required");

            using var httpClient = new HttpClient();
            var imageBytes = await httpClient.GetByteArrayAsync(user.ProfilePicture);

            return new ProfileImageResult("profile-pic.png", "image/png", Convert.ToBase64String(imageBytes));
        }
    }
}
