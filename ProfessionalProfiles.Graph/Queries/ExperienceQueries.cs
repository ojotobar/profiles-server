using CSharpTypes.Extensions.Guid;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class ExperienceQueries
    {
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
    }
}
