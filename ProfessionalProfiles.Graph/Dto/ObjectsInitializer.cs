using CSharpTypes.Extensions.Guid;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.CareerSummaries;
using ProfessionalProfiles.Graph.Certfications;
using ProfessionalProfiles.Graph.Experiences;
using ProfessionalProfiles.Graph.Projects;
using ProfessionalProfiles.Graph.Skills;
using ProfessionalProfiles.Shared.Extensions;

namespace ProfessionalProfiles.Graph.Dto
{
    internal static class ObjectsInitializer
    {
        public static WorkExperience Map(this ExperienceInput input, WorkExperience existing)
        {
            existing.Organization = input.Organization;
            existing.JobTitle = input.Title;
            existing.StartDate = input.StartDate;
            existing.EndDate = input.EndDate;
            existing.Accomplishments = input.Summaries ?? [];
            existing.Location.City = input.Location.City;
            existing.Location.Country = input.Location.Country;
            existing.UpdatedOn = DateTime.UtcNow;
            return existing;
        }

        public static List<WorkExperience> Initialize(this List<ExperienceInput> inputs, Guid userId)
        {
            var results = new List<WorkExperience>();
            inputs.ForEach(input => results.Add(new WorkExperience
            {
                Organization = input.Organization,
                JobTitle = input.Title,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                Accomplishments = input.Summaries ?? [],
                Location = new EntityLocation
                {
                    City = input.Location.City,
                    Country = input.Location.Country
                },
                UserId = userId
            }));
            return results;
        }

        public static ProfessionalSummary Map(this ProfessionalSummaryInput input, ProfessionalSummary existing)
        {
            existing.Summary = input.Summary;
            existing.UpdatedOn = DateTime.UtcNow;
            return existing;
        }

        public static List<Project> Initialize(this List<ProjectInput> inputs, Guid userId)
        {
            var results = new List<Project>();
            inputs.ForEach(input => results.Add(new Project
            {
                Name = input.ProjectName,
                Link = input.Link ?? string.Empty,
                Description = input.Summary,
                Technologies = input.Technologies ?? [],
                UserId = userId
            }));
            return results;
        }

        public static Project Map(this ProjectInput input, Project existing)
        {
            existing.Name = input.ProjectName;
            existing.Technologies = input.Technologies ?? [];
            existing.Description = input.Summary;
            existing.Link = input.Link ?? string.Empty;
            existing.UpdatedOn = DateTime.UtcNow;
            return existing;
        }

        public static ProfessionalSummary Initialize(this ProfessionalSummaryInput input, Guid userId)
            => new()
            {
                Summary = input.Summary,
                UserId = userId
            };

        public static Certification Map(this CertificationInput input, Certification existing)
        {
            existing.Name = input.Name;
            existing.Institution = input.InstitutionName;
            existing.DateObtained = input.Date;
            existing.Expires = input.YearsOfValidity.HasValue
                    ? input.Date.AddYears(input.YearsOfValidity.Value)
                    : null;
            existing.UpdatedOn = DateTime.UtcNow;
            existing.Link = input.Link;
            return existing;
        }

        public static List<Certification> Initialize(this List<CertificationInput> inputs, Guid userId)
        {
            var results = new List<Certification>();
            inputs.ForEach(input => results.Add(new Certification
            {
                Name = input.Name,
                Institution = input.InstitutionName,
                UserId = userId,
                DateObtained = input.Date,
                Link = input.Link,
                Expires = input.YearsOfValidity.HasValue
                    ? input.Date.AddYears(input.YearsOfValidity.Value)
                    : null
            }));
            return results;
        }

        public static List<Skill> Map(this List<SkillInput> inputs, Guid userId)
        {
            var outputs = new List<Skill>();
            foreach (var input in inputs)
            {
                outputs.Add(new Skill
                {
                    Name= input.Name.CapitalizeText(),
                    Level= input.Level,
                    YearsOfExperience = input.Years,
                    Certified = input.IsCertified,
                    UserId = userId
                });
            }
            return outputs;
        }

        public static void Map(this SkillInput input, Skill existingSkill)
        {
            var outputs = new List<Skill>();
            existingSkill.Name = input.Name.CapitalizeText();
            existingSkill.Level = input.Level;
            existingSkill.YearsOfExperience = input.Years;
            existingSkill.Certified = input.IsCertified;
            existingSkill.UpdatedOn = DateTime.UtcNow;
        }

        public static async Task<(int Progress, bool CanGenerate)> CanGenerateApiKey(this IRepositoryManager repository, 
            bool isEmailConfirmed, bool hasLocationAdded, bool hasProfilePics, bool hasCvAdded)
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
                progress += 10;
            }

            var userId = repository.User.GetLoggedInOrApiKeyUserId("");
            if (userId.IsEmpty())
            {
                return (progress, false);
            }

            var hasEducation = await repository.Education.HasAnyAsync(e => e.UserId.Equals(userId));
            if(hasEducation)
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
