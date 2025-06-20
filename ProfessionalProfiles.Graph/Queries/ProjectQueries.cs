using CSharpTypes.Extensions.Guid;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class ProjectQueries
    {
        /// <summary>
        /// Get Project by id
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<Project?> GetProjectAsync(Guid id, [Service] IRepositoryManager repository,
            [GlobalState] ApiAccessInput apiAccessInput)
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiAccessInput);
            if (userId.IsEmpty())
            {
                return null;
            }

            return await repository.Project
                .FindAsync(c => !c.IsDeprecated && c.Id.Equals(id) && c.UserId.Equals(userId));
        }

        /// <summary>
        /// Gets a list of user's Projects
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<IQueryable<Project>> GetProjects([Service] IRepositoryManager repository,
            [GlobalState] ApiAccessInput apiAccessInput)
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiAccessInput);
            if (userId.IsEmpty())
            {
                return new List<Project>().AsQueryable();
            }

            return repository.Project
                .FindAsQueryable(c => !c.IsDeprecated && c.UserId.Equals(userId));
        }
    }
}
