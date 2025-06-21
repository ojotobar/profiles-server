using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Account;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.General;
using ProfessionalProfiles.Shared.Extensions;
using System.Net;

namespace ProfessionalProfiles.Graph.Extensions
{
    public static class GraphServiceExtensions
    {
        private const long MAX_IMAGE_SIZE = 3145368; //3mb
        private const long MAX_DOC_SIZE = 524288;//500kb
        private static readonly List<string> ALLOWEDIMAGEFORMATS = [".png", ".jpg", ".jpeg"];
        private static  readonly List<string> ALLOWEDDOCFORMATS = [".pdf", ".docx", ".doc"];

        public static async Task<ProfileSummaryDto> GetProfileSummary(this IRepositoryManager repository, Professional user)
        {
            var educations = await repository.Education.CountAllAsync(e => e.UserId.Equals(user.Id) && !e.IsDeprecated);
            var experiences = await repository.WorkExperience.CountAllAsync(we => we.UserId.Equals(user.Id) && !we.IsDeprecated);
            var skills = await repository.Skill.CountAllAsync(s => s.UserId.Equals(user.Id) && !s.IsDeprecated);
            var projects = await repository.Project.CountAllAsync(p => p.UserId.Equals(user.Id) && !p.IsDeprecated);
            var certs = await repository.Certification.CountAllAsync(c => c.UserId.Equals(user.Id) && !c.IsDeprecated);
            var hasSummary = await repository.Summary.HasAsync(cs => cs.UserId.Equals(user.Id) && !cs.IsDeprecated);

            var canGenerate = await repository.CanGenerateApiKey(user.EmailConfirmed,
                user.Location != null, !string.IsNullOrWhiteSpace(user.ProfilePicture), !string.IsNullOrWhiteSpace(user.ResumeLink), user.SocialMedia.IsNotNullOrEmpty());

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
            var summary = await repository.Summary.FindAsync(cs => cs.UserId.Equals(user.Id) && !cs.IsDeprecated);

            return new ProfileSummary(user.FirstName, user.LastName, user.ProfilePicture ?? "", summary?.Heading ?? "", 
                hasXp, hasSkills, hasEducation, hasProject, hasCerts, user.SocialMedia.ToList());
        }

        public static async Task<ProfileSummaryLean> GetProfileSummary(this IRepositoryManager repository, Guid userId)
        {
            var educations = await repository.Education.CountAllAsync(e => e.UserId.Equals(userId) && !e.IsDeprecated);
            var skills = await repository.Skill.FindRangeAsync(s => s.UserId.Equals(userId) && !s.IsDeprecated);
            var projects = await repository.Project.CountAllAsync(p => p.UserId.Equals(userId) && !p.IsDeprecated);
            var certs = await repository.Certification.CountAllAsync(c => c.UserId.Equals(userId) && !c.IsDeprecated);
            var profileSummary = await repository.Summary.FindAsync(cs => cs.UserId.Equals(userId) && !cs.IsDeprecated);
            var xp = await repository.WorkExperience.FindRangeAsync(we => we.UserId.Equals(userId) && !we.IsDeprecated);
            var yearsOfXp = GetYearsOfExperience(xp);

            return new ProfileSummaryLean(profileSummary?.Heading ?? "", (int)yearsOfXp, skills.OrderByDescending(s => s.Level).Select(s => s.Name).ToList(), skills.Count, educations, projects, certs);
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

            var userId = await repository.User.GetLoggedInOrApiKeyUserId();
            if (userId.IsEmpty())
            {
                return (progress, false);
            }

            var hasEducation = await repository.Education.HasAnyAsync(e => e.UserId.Equals(userId) && !e.IsDeprecated);
            if (hasEducation)
            {
                progress += 10;
            }

            var hasExperience = await repository.WorkExperience.HasAnyAsync(xp => xp.UserId.Equals(userId) && !xp.IsDeprecated);
            if (hasExperience)
            {
                progress += 10;
            }

            var hasSkills = await repository.Skill.HasAnyAsync(sk => sk.UserId.Equals(userId) && !sk.IsDeprecated);
            if (hasSkills)
            {
                progress += 10;
            }

            var hasProjects = await repository.Project.HasAnyAsync(pro => pro.UserId.Equals(userId) && !pro.IsDeprecated);
            if (hasProjects)
            {
                progress += 10;
            }

            var hasCert = await repository.Certification.HasAnyAsync(cert => cert.UserId.Equals(userId) && !cert.IsDeprecated);
            if (hasCert)
            {
                progress += 10;
            }

            var hasSummary = await repository.Summary.HasAsync(s => s.UserId.Equals(userId) && !s.IsDeprecated);
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

        public static UserCommonPayload ValidateImageFile(this IFile file)
        {
            if (file.IsNull() || file.Length <= 0)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", "Invalid file", HttpStatusCode.BadRequest));
            }

            if (file.Length > MAX_IMAGE_SIZE)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", $"File size exceeds limit of {MAX_IMAGE_SIZE / 1024}kb", HttpStatusCode.BadRequest));
            }

            if (!ALLOWEDIMAGEFORMATS.Any(f => file.Name.EndsWith(f)))
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", $"Invalid image format. Allowed formats: {string.Join(", ", ALLOWEDIMAGEFORMATS)}", HttpStatusCode.BadRequest));
            }

            return new UserCommonPayload(UserGenericPayload.Initialize("", "", HttpStatusCode.OK, true));
        }

        public static UserCommonPayload ValidateDocFiles(this IFile file)
        {
            if (file.IsNull() || file.Length <= 0)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", "Invalid file", HttpStatusCode.BadRequest));
            }

            if (file.Length > MAX_DOC_SIZE)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", $"File size exceeds limit of {MAX_DOC_SIZE / 1024}kb", HttpStatusCode.BadRequest));
            }

            if (!ALLOWEDDOCFORMATS.Any(f => file.Name.EndsWith(f)))
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", $"Invalid document format. Allowed formats: {string.Join(", ", ALLOWEDIMAGEFORMATS)}", HttpStatusCode.BadRequest));
            }

            return new UserCommonPayload(UserGenericPayload.Initialize("", "", HttpStatusCode.OK, true));
        }

        public static async Task<UserValidationPayload> ValidateLoggedinUser(this string userId, UserManager<Professional> userManager)
        {
            if (userId.IsNullOrEmpty())
            {
                return new UserValidationPayload(null, "Access denied", HttpStatusCode.Unauthorized);
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new UserValidationPayload(null, "No user found", HttpStatusCode.NotFound);
            }

            return new UserValidationPayload(user, "", HttpStatusCode.OK, true);
        }

        public static string GetFileName(this Professional user, IFile file, ECloudFolder folder)
        {
            string ext = System.IO.Path.GetExtension(file.Name);
            var fileName = file.Name.Replace(" ", "_");
            if (ext.IsNotNullOrEmpty())
            {
                var base64EncodedStr = user.Id.EncodeGuidAsBase64();
                fileName = $"{base64EncodedStr}_{folder}{ext}";
            }

            return fileName;
        }

        public static string GetFileName(this Professional user, ECloudFolder folderType)
        {
            var file = folderType == ECloudFolder.ProfilePics ?
                user.ProfilePicture : user.ResumeLink;

            if (!string.IsNullOrWhiteSpace(file))
            {
                var base64IdString = user.Id.EncodeGuidAsBase64();
                var fileNameWithoutExt = $"{base64IdString}_{folderType}";
                var fileNameSplit = file.Split($"{fileNameWithoutExt}");
                if (fileNameSplit.Length > 1)
                {
                    var extSplit = fileNameSplit[1].Split('?');
                    if (extSplit.Length > 0)
                    {
                        return $"{fileNameWithoutExt}{extSplit[0]}";
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    return string.Empty;
                }
            }

            return string.Empty;
        }

        #region Private methods
        private static double GetYearsOfExperience(List<WorkExperience>? experiences)
        {
            var result = 0;
            var days = 0;

            if (experiences == null || experiences.Count == 0)
            {
                return result;
            }

            foreach (var experience in experiences)
            {
                days += ((experience.EndDate ?? DateTime.UtcNow) - experience.StartDate).Days;
            }

            return Math.Round(days / 365.25);
        }
        #endregion
    }
}
