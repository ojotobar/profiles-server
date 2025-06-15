using CSharpTypes.Extensions.Guid;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class CertificationQueries
    {
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
    }
}
