using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.List;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Shared.Extensions;

namespace ProfessionalProfiles.Graph.Extensions
{
    public static class GraphServiceExtensions
    {
        public static async Task<ProfileSummaryDto> GetProfileSummary(this IRepositoryManager repository, Professional user)
        {
            var educations = await repository.Education.CountAllAsync(e => e.UserId.Equals(user.Id) && !e.IsDeprecated);
            var experiences = await repository.WorkExperience.CountAllAsync(we => we.UserId.Equals(user.Id) && !we.IsDeprecated);
            var skills = await repository.Skill.CountAllAsync(s => s.UserId.Equals(user.Id) && !s.IsDeprecated);
            var projects = await repository.Project.CountAllAsync(p => p.UserId.Equals(user.Id) && !p.IsDeprecated);
            var certs = await repository.Certification.CountAllAsync(c => c.UserId.Equals(user.Id) && !c.IsDeprecated);
            var hasSummary = await repository.Summary.HasAsync(cs => cs.UserId.Equals(user.Id) && !cs.IsDeprecated);

            var canGenerate = await repository.CanGenerateApiKey(user.EmailConfirmed,
                user.Location != null, user.ProfilePicture != null, user.ResumeLink != null, user.SocialMedia.IsNotNullOrEmpty());

            var apiKey = "";
            if (user.KeyMarker != default)
            {
                apiKey = user.Id.EncodeGuidAsBase64(user.KeyMarker);
            }

            return new ProfileSummaryDto(educations, experiences, skills, projects, certs,
                hasSummary, canGenerate.Progress, canGenerate.CanGenerate, apiKey);
        }

        public static async Task<ProfileSummary> GetProfileSummaryForMenus(this IRepositoryManager repository, Professional user)
        {
            var hasEducation = await repository.Education.HasAnyAsync(e => e.UserId.Equals(user.Id) && !e.IsDeprecated);
            var hasXp = await repository.WorkExperience.HasAnyAsync(we => we.UserId.Equals(user.Id) && !we.IsDeprecated);
            var hasSkills = await repository.Skill.HasAnyAsync(s => s.UserId.Equals(user.Id) && !s.IsDeprecated);
            var hasProject = await repository.Project.HasAnyAsync(p => p.UserId.Equals(user.Id) && !p.IsDeprecated);
            var hasCerts = await repository.Certification.HasAnyAsync(c => c.UserId.Equals(user.Id) && !c.IsDeprecated);
            var hasSummary = await repository.Summary.HasAsync(cs => cs.UserId.Equals(user.Id) && !cs.IsDeprecated);

            return new ProfileSummary(user.FirstName, user.LastName, user.ProfilePicture ?? "", "", 
                hasXp, hasSkills, hasEducation, hasProject, hasCerts, user.SocialMedia.ToList());
        }

        public static async Task<(int Progress, bool CanGenerate)> CanGenerateApiKey(this IRepositoryManager repository,
            bool isEmailConfirmed, bool hasLocationAdded, bool hasProfilePics, bool hasCvAdded, bool hasSocialMedia)
        {
            const int threshhold = 80;
            int progress = 10;
            if (isEmailConfirmed)
            {
                progress += 10;
            }

            if (hasCvAdded)
            {
                progress += 5;
            }

            if (hasProfilePics)
            {
                progress += 5;
            }

            if (hasLocationAdded)
            {
                progress += 5;
            }

            if (hasSocialMedia)
            {
                progress += 5;
            }

            var userId = repository.User.GetLoggedInOrApiKeyUserId("");
            if (userId.IsEmpty())
            {
                return (progress, false);
            }

            var hasEducation = await repository.Education.HasAnyAsync(e => e.UserId.Equals(userId));
            if (hasEducation)
            {
                progress += 10;
            }

            var hasExperience = await repository.WorkExperience.HasAnyAsync(xp => xp.UserId.Equals(userId));
            if (hasExperience)
            {
                progress += 10;
            }

            var hasSkills = await repository.Skill.HasAnyAsync(sk => sk.UserId.Equals(userId));
            if (hasSkills)
            {
                progress += 10;
            }

            var hasProjects = await repository.Project.HasAnyAsync(pro => pro.UserId.Equals(userId));
            if (hasProjects)
            {
                progress += 10;
            }

            var hasCert = await repository.Certification.HasAnyAsync(cert => cert.UserId.Equals(userId));
            if (hasCert)
            {
                progress += 10;
            }

            var hasSummary = await repository.Summary.HasAsync(s => s.UserId.Equals(userId));
            if (hasSummary)
            {
                progress += 10;
            }

            return
                (
                    progress,
                    isEmailConfirmed && hasCvAdded && hasProfilePics && hasLocationAdded &&
                    hasEducation && hasExperience && hasSkills && hasSummary && progress >= threshhold
                );
        }
    }
}
