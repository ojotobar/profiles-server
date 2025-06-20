using CSharpTypes.Extensions.Guid;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class EducationQueries
    {
        /// <summary>
        /// Gets a list of user edication records
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<IQueryable<Education>> GetEducations([Service] IRepositoryManager repository,
            [GlobalState] string? apiKey = "", [GlobalState] string? clientTag = "")
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiKey!, clientTag!);
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
            [GlobalState] string? apiKey = "", [GlobalState] string? clientTag = "")
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiKey!, clientTag!);
            if (userId.IsEmpty())
            {
                return null;
            }

            return await repository.Education
                .FindOneAsync(e => e.Id == id && !e.IsDeprecated);
        }
    }
}
