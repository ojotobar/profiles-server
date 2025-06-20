using CSharpTypes.Extensions.Guid;
using HotChocolate.Authorization;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class PortfolioVersionQueries
    {
        /// <summary>
        /// Get a portfolio version by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<PortfolioVersion?> GetPortfolioVersionAsync(Guid id, [Service] IRepositoryManager repository)
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId();
            if (userId.IsEmpty())
            {
                return null;
            }

            return await repository.PortfolioVersion
                .FindAsync(c => !c.IsDeprecated && c.Id.Equals(id));
        }

       /// <summary>
       /// Gets a list of all portfolio versions
       /// </summary>
       /// <param name="repository"></param>
       /// <returns></returns>
        public IQueryable<PortfolioVersion> GetPortfolioVersions([Service] IRepositoryManager repository)
        {
            return repository.PortfolioVersion
                .FindAsQueryable(c => !c.IsDeprecated);
        }
    }
}
