using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.String;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Win32;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Account;
using ProfessionalProfiles.Graph.Dto;
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
                return new ApiKeyPayload("", "Access denied");
            }

            var user = await userManager.FindByIdAsync(loggedInUserId);
            if (user == null)
            {
                return new ApiKeyPayload("", "User not found");
            }

            var canGenerate = await repository.CanGenerateApiKey(user.EmailConfirmed,
                user.Location != null, user.ProfilePicture != null, user.ResumeLink != null);

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
            [Service] IRepositoryManager repository, [GlobalState] string? apiKey = "")
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);

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
        /// Gets logged in user's profile summary
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ProfileSummaryDto?> GetUserSummaryAsync([Service] IRepositoryManager repository,
            [Service] UserManager<Professional> userManager)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId("");
            if (userId.IsEmpty())
            {
                return null;
            }

            var user = await userManager.FindByIdAsync(userId.ToString());
            if(user == null)
            {
                return null;
            }

            var educations = await repository.Education.CountAllAsync(e => e.UserId.Equals(userId));
            var experiences = await repository.WorkExperience.CountAllAsync(we => we.UserId.Equals(userId));
            var skills = await repository.Skill.CountAllAsync(s => s.UserId.Equals(userId));
            var projects = await repository.Project.CountAllAsync(p => p.UserId.Equals(userId));
            var certs = await repository.Certification.CountAllAsync(c => c.UserId.Equals(userId));
            var hasSummary = await repository.Summary.HasAsync(cs => cs.UserId.Equals(userId));

            var canGenerate = await repository.CanGenerateApiKey(user.EmailConfirmed, 
                user.Location != null, user.ProfilePicture != null, user.ResumeLink != null);

            var apiKey = "";
            if(user.KeyMarker != default)
            {
                apiKey = user.Id.EncodeGuidAsBase64(user.KeyMarker);
            }

            return new ProfileSummaryDto(educations, experiences, skills, projects, certs, 
                hasSummary, canGenerate.Progress, canGenerate.CanGenerate, apiKey);
        }
        #endregion

        #region Education Section
        /// <summary>
        /// Gets a list of user edication records
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public IQueryable<Education> GetEducations([Service] IRepositoryManager repository,
            [GlobalState] string? apiKey = "")
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new List<Education>().AsQueryable();
            }

            return repository.Education
                .FindAsQueryable(e => !e.IsDeprecated && e.UserId.Equals(userId))
                .OrderByDescending(e => e.StartDate);
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

            return await repository.Education
                .FindOneAsync(e => e.Id == id && !e.IsDeprecated);
        }
        #endregion

        #region Certification Section
        /// <summary>
        /// Get certification by id
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<Certification?> GetCertificationAsync(Guid id, [Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return null;
            }

            return await repository.Certification
                .FindAsync(c => !c.IsDeprecated && c.Id.Equals(id));
        }

        /// <summary>
        /// Gets a list of user's certification
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public IQueryable<Certification> GetCertifications([Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new List<Certification>().AsQueryable();
            }

            return repository.Certification
                .FindAsQueryable(c => !c.IsDeprecated && c.UserId.Equals(userId));
        }
        #endregion

        #region Professional Summary
        /// <summary>
        /// Get Professional Summary by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ProfessionalSummary?> GetProfessionalSummaryByIdAsync(Guid id, [Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return null;
            }

            return await repository.Summary
                .FindAsync(s => !s.IsDeprecated && s.Id.Equals(id) && s.UserId.Equals(userId));
        }

        /// <summary>
        /// Get Professional Summary by User
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<ProfessionalSummary?> GetProfessionalSummaryAsync([Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return null;
            }

            return await repository.Summary
                .FindAsync(s => !s.IsDeprecated && s.UserId.Equals(userId));
        }
        #endregion

        #region Project Section
        /// <summary>
        /// Get Project by id
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<Project?> GetProjectAsync(Guid id, [Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return null;
            }

            return await repository.Project
                .FindAsync(c => !c.IsDeprecated && c.Id.Equals(id) && c.UserId.Equals(userId));
        }

        /// <summary>
        /// Gets a list of user's Projects
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public IQueryable<Project> GetProjects([Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new List<Project>().AsQueryable();
            }

            return repository.Project
                .FindAsQueryable(c => !c.IsDeprecated && c.UserId.Equals(userId));
        }
        #endregion

        #region Experience Section
        /// <summary>
        /// Get Experience by id
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<WorkExperience?> GetExperienceAsync(Guid id, [Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return null;
            }

            return await repository.WorkExperience
                .FindAsync(c => !c.IsDeprecated && c.Id.Equals(id) && c.UserId.Equals(userId));
        }

        /// <summary>
        /// Gets a list of user's Experience
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public IQueryable<WorkExperience> GetExperiences([Service] IRepositoryManager repository,
            [GlobalState] string? apiKey)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);
            if (userId.IsEmpty())
            {
                return new List<WorkExperience>().AsQueryable();
            }

            return repository.WorkExperience
                .FindAsQueryable(c => !c.IsDeprecated && c.UserId.Equals(userId))
                .OrderByDescending(c => c.StartDate);
        }
        #endregion

        #region Skills Section
        /// <summary>
        /// Get User Skills
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public IQueryable<Skill> GetSkills([Service] IRepositoryManager repository, [GlobalState] string? apiKey = "")
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);

            if (userId.IsEmpty())
            {
                return new List<Skill>().AsQueryable();
            }

            return repository.Skill
                .FindAsQueryable(s => s.UserId.Equals(userId))
                .OrderBy(s => s.Name)
                .ThenByDescending(s => s.Level);
        }

        /// <summary>
        /// Get User Skill By Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<Skill?> GetSkillAsync(Guid id, [Service] IRepositoryManager repository, [GlobalState] string? apiKey = "")
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId(apiKey!);

            if (userId.IsEmpty())
            {
                return null;
            }

            return await repository.Skill.FindAsync(s => s.Id.Equals(id));
        }

        /// <summary>
        /// Gets user skills count
        /// </summary>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<int> GetSkillsCountAsync([Service] IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId("");

            if (userId.IsEmpty())
            {
                return default;
            }

            return (int)(await repository.Skill.CountAllAsync(s => s.UserId.Equals(userId)));
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
            return repository.Faqs.FindAsQueryable(f => !f.IsDeprecated);
        }
        #endregion
    }
}
