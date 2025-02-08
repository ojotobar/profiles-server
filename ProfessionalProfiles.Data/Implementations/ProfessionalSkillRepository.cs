using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class ProfessionalSkillRepository(MongoDbSettings settings) 
        : Repository<ProfessionalSkill>(settings), IProfessionalSkillRepository
    {
        public async Task<ProfessionalSkill?> FindAsync(Expression<Func<ProfessionalSkill, bool>> expression)
            => await GetAsync(expression);

        public IQueryable<ProfessionalSkill> FindAsQueryable(Expression<Func<ProfessionalSkill, bool>> expression)
            => GetAsQueryable(expression);

        public async Task<List<ProfessionalSkill>> FindRangeAsync(Expression<Func<ProfessionalSkill, bool>> expression)
            => await GetManyAsync(expression);

        public async Task AddAsync(ProfessionalSkill professionalSkill)
            => await CreateAsync(professionalSkill);

        public async Task AddRangeAsync(List<ProfessionalSkill> professionalSkills)
            => await CreateManyAsync(professionalSkills);

        public async Task EditAsync(Expression<Func<ProfessionalSkill, bool>> expression
            , ProfessionalSkill professionalSkill)
            => await UpdateAsync(expression, professionalSkill);

        public async Task DeleteAsync(Expression<Func<ProfessionalSkill, bool>> expression)
            => await RemoveAsync(expression);

        public async Task DeleteRangeAsync(Expression<Func<ProfessionalSkill, bool>> expression,
            CancellationToken token)
            => await RemoveManyAsync(expression, token);

        public async Task<bool> HasAnyAsync(Expression<Func<ProfessionalSkill, bool>> expression)
            => await ExistsAsync(expression);
    }
}
