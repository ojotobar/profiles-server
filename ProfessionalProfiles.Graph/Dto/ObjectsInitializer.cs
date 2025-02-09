using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.CareerSummaries;
using ProfessionalProfiles.Graph.Certfications;
using ProfessionalProfiles.Graph.Experiences;
using ProfessionalProfiles.Graph.Projects;
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
                Technologoes = input.Technologies ?? [],
                UserId = userId
            }));
            return results;
        }

        public static Project Map(this ProjectInput input, Project existing)
        {
            existing.Name = input.ProjectName;
            existing.Technologoes = input.Technologies ?? [];
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
                Expires = input.YearsOfValidity.HasValue
                    ? input.Date.AddYears(input.YearsOfValidity.Value)
                    : null
            }));
            return results;
        }

        public static List<string> Map(this List<string> inputs)
        {
            var outputs = new List<string>();
            foreach (var input in inputs)
            {
                outputs.Add(input.CapitalizeText());
            }
            return outputs;
        }
    }
}
