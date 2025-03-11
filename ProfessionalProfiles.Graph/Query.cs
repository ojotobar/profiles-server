using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.String;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Account;
using ProfessionalProfiles.Graph.CareerSummaries;
using ProfessionalProfiles.Graph.Certfications;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.Experiences;
using ProfessionalProfiles.Graph.Projects;
using ProfessionalProfiles.GraphQL.Profile;
using ProfessionalProfiles.Shared.Extensions;
using System.Net;

namespace ProfessionalProfiles.Graph
{
    public class Query
    {
        #region Profile Section
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

        /// <summary>
        /// Gets User profile details
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public async Task<ProfilePayload> GetProfileAsync([Service] UserManager<Professional> userManager,
            [Service] IRepositoryManager repository, [GlobalState] string? apiKey = "")
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);

            if (userId.IsEmpty())
            {
                return new ProfilePayload(ProfessionalDto.MapData(null), "Access denied");
            }

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new ProfilePayload(ProfessionalDto.MapData(user), "User not found!");
            }

            return new ProfilePayload(ProfessionalDto.MapData(user), "Profile retrieved successfully", true);
        }
        #endregion

        #region Education Section
        /// <summary>
        /// Gets a list of user edication records
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<List<Education>> GetEducationsAsync([Service] IRepositoryManager repository,
            [GlobalState] string? apiKey = "")
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return [];
            }

            return await Task.Run(() => repository.Education
                .FindAsQueryable(e => !e.IsDeprecated && e.UserId.Equals(userId.ToString()))
                .OrderByDescending(e => e.StartDate)
                .ToList());
        }

        /// <summary>
        /// Gets education by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<Education?> GetEducationAsync(Guid id, [Service] IRepositoryManager repository,
            [GlobalState] string? apiKey = "")
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return null;
            }

            var record = await repository.Education.FindOneAsync(e => e.Id == id && !e.IsDeprecated);
            if (record == null)
            {
                return null;
            }

            return record;
        }
        #endregion

        #region Certification Section
        /// <summary>
        /// Get certification by id
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<CertificationPayload> GetCertificationAsync(Guid id, [Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new CertificationPayload(null, "Access denied!!!");
            }

            var certification = await repository.Certification
                .FindAsync(c => !c.IsDeprecated && c.Id.Equals(id));

            return new CertificationPayload(certification, "Record retrieved successfully", true);
        }

        /// <summary>
        /// Gets a list of user's certification
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<CertificationsPayload> GetCertificationsAsync([Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new CertificationsPayload([], "Access denied!!!");
            }

            var certifications = await repository.Certification
                .FindRangeAsync(c => !c.IsDeprecated && c.UserId.Equals(userId));

            return new CertificationsPayload(certifications, "Records retrieved successfully", true);
        }
        #endregion

        #region Professional Summary
        /// <summary>
        /// Get Professional Summary by id
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ProfessionalSummaryPayload> GetProfessionalSummaryAsync(Guid id, [Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new ProfessionalSummaryPayload(null, "Access denied!!!");
            }

            var summary = await repository.Summary
                .FindAsync(s => !s.IsDeprecated && s.Id.Equals(id) && s.UserId.Equals(userId));

            return new ProfessionalSummaryPayload(summary, "Record retrieved successfully", true);
        }
        #endregion

        #region Project Section
        /// <summary>
        /// Get Project by id
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ProjectPayload> GetProjectAsync(Guid id, [Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new ProjectPayload(null, "Access denied!!!");
            }

            var project = await repository.Project
                .FindAsync(c => !c.IsDeprecated && c.Id.Equals(id) && c.UserId.Equals(userId));

            return new ProjectPayload(project, "Record retrieved successfully", true);
        }

        /// <summary>
        /// Gets a list of user's Projects
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ProjectsPayload> GetProjectsAsync([Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new ProjectsPayload([], "Access denied!!!");
            }

            var certifications = await repository.Project
                .FindRangeAsync(c => !c.IsDeprecated && c.UserId.Equals(userId));

            return new ProjectsPayload(certifications, "Records retrieved successfully", true);
        }
        #endregion

        #region Experience Section
        /// <summary>
        /// Get Experience by id
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ExperiencePayload> GetExperienceAsync(Guid id, [Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new ExperiencePayload(null, "Access denied!!!");
            }

            var experience = await repository.WorkExperience
                .FindAsync(c => !c.IsDeprecated && c.Id.Equals(id) && c.UserId.Equals(userId));

            return new ExperiencePayload(experience, "Record retrieved successfully", true);
        }

        /// <summary>
        /// Gets a list of user's Experience
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ExperiencesPayload> GetExperiencesAsync([Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new ExperiencesPayload([], "Access denied!!!");
            }

            var experiences = await repository.WorkExperience
                .FindRangeAsync(c => !c.IsDeprecated && c.UserId.Equals(userId));

            return new ExperiencesPayload(experiences, "Records retrieved successfully", true);
        }
        #endregion

        #region FAQs
        /// <summary>
        /// Gets paginated list of FAQs
        /// </summary>
        /// <param name="repository"></param>
        /// <returns></returns>
        [UseOffsetPaging(IncludeTotalCount = true)]
        public IQueryable<Faqs> GetFaqs([Service] IRepositoryManager repository)
        {
            return repository.Faqs.FindAsQueryable(f => !f.IsDeprecated)
                .OrderBy(f => f.CreatedOn);
        }
        #endregion
    }
}
