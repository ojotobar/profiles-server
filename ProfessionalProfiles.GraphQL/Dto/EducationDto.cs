using Mailjet.Client.Resources;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.GraphQL.Educations;
using System.Reflection.Emit;

namespace ProfessionalProfiles.GraphQL.Dto
{
    public class EducationDto
    {
        public static Education CreateMap(string userId, EducationInput input)
        {
            return new Education
            {
                InstitutionName = input.SchoolName,
                Level = input.Level,
                Major = input.Course,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                UserId = userId,
                Location = new EntityLocation
                {
                    City = input.Location.City,
                    Country = input.Location.Country,
                    State = input.Location.State,
                    Longitude = input.Location.Longitude,
                    Latitude = input.Location.Latitude
                }
            };
        }

        public static Education CreateMap(Education recordToUpdate, EducationInput input)
        {
            recordToUpdate.InstitutionName = input.SchoolName;
            recordToUpdate.Level = input.Level;
            recordToUpdate.Major = input.Course;
            recordToUpdate.StartDate = input.StartDate;
            recordToUpdate.EndDate = input.EndDate;
            recordToUpdate.UpdatedOn = DateTime.UtcNow;
            recordToUpdate.Location = new EntityLocation
            {
                City = input.Location.City,
                Country = input.Location.Country,
                State = input.Location.State,
                Longitude = input.Location.Longitude,
                Latitude = input.Location.Latitude
            };
            return recordToUpdate;
        }
    }
}
