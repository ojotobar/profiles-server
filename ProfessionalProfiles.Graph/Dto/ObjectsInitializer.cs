using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Certfications;

namespace ProfessionalProfiles.Graph.Dto
{
    internal static class ObjectsInitializer
    {
        public static Certification Initialize(this CertificationInput input, Guid userId)
            => new()
            {
                Name = input.Name,
                Institution = input.InstitutionName,
                UserId = userId,
                DateObtained = input.Date,
                Expires = input.YearsOfValidity.HasValue 
                    ? input.Date.AddYears(input.YearsOfValidity.Value) 
                    : null
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
    }
}
