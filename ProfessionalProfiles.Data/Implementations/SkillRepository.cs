using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class SkillRepository(MongoDbSettings settings) 
        : Repository<Skill>(settings), ISkillRepository
    {
        public async Task<Skill?> FindAsync(Expression<Func<Skill, bool>> expression)
            => await GetAsync(expression);

        public IQueryable<Skill> FindAsQueryable(Expression<Func<Skill, bool>> expression)
            => GetAsQueryable(expression);

        public async Task<List<Skill>> FindRangeAsync(Expression<Func<Skill, bool>> expression)
            => await GetManyAsync(expression);

        public async Task AddAsync(Skill skill)
            => await CreateAsync(skill);

        public async Task AddRangeAsync(List<Skill> skills)
            => await CreateManyAsync(skills);

        public async Task EditAsync(Expression<Func<Skill, bool>> expression
            , Skill skill)
            => await UpdateAsync(expression, skill);

        public async Task DeleteAsync(Expression<Func<Skill, bool>> expression)
            => await RemoveAsync(expression);

        public async Task DeleteRangeAsync(Expression<Func<Skill, bool>> expression,
            CancellationToken token)
            => await RemoveManyAsync(expression, token);

        public async Task<bool> HasAnyAsync(Expression<Func<Skill, bool>> expression)
            => await ExistsAsync(expression);

        public async Task<long> CountAllAsync(Expression<Func<Skill, bool>> expression) =>
            await CountAsync(expression);
    }
}
