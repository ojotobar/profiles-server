using CSharpTypes.Extensions.Guid;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class ProfessionalSummaryQueries
    {
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
    }
}
