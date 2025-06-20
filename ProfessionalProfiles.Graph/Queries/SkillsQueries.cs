using CSharpTypes.Extensions.Guid;
using HotChocolate.Authorization;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class SkillsQueries
    {
        /// <summary>
        /// Get User Skills
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<IQueryable<Skill>> GetSkills([Service] IRepositoryManager repository, 
            [GlobalState] string? apiKey = "", [GlobalState] string? clientTag = "")
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiKey!, clientTag!);

            if (userId.IsEmpty())
            {
                return new List<Skill>().AsQueryable();
            }

            return repository.Skill
                .FindAsQueryable(s => s.UserId.Equals(userId))
                .OrderBy(s => s.Name)
                .ThenByDescending(s => s.Level);
        }

        /// <summary>
        /// Get User Skill By Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <param name="apiKey"></param>
        /// <returns></returns>
        public async Task<Skill?> GetSkillAsync(Guid id, [Service] IRepositoryManager repository, 
            [GlobalState] string? apiKey = "", [GlobalState] string? clientTag = "")
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId(apiKey!, clientTag!);

            if (userId.IsEmpty())
            {
                return null;
            }

            return await repository.Skill.FindAsync(s => s.Id.Equals(id));
        }

        /// <summary>
        /// Gets user skills count
        /// </summary>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<int> GetSkillsCountAsync([Service] IRepositoryManager repository)
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId("");

            if (userId.IsEmpty())
            {
                return default;
            }

            return (int)(await repository.Skill.CountAllAsync(s => s.UserId.Equals(userId)));
        }
    }
}
