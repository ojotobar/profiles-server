using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class WorkExperienceRepository(MongoDbSettings settings) 
        : Repository<WorkExperience>(settings), IWorkExperienceRepository
    {
        public async Task<WorkExperience?> FindAsync(Expression<Func<WorkExperience, bool>> expression)
            => await GetAsync(expression);

        public IQueryable<WorkExperience> FindAsQueryable(Expression<Func<WorkExperience, bool>> expression)
            => GetAsQueryable(expression);

        public async Task<List<WorkExperience>> FindRangeAsync(Expression<Func<WorkExperience, bool>> expression)
            => await GetManyAsync(expression);

        public async Task AddAsync(WorkExperience workExperience)
            => await CreateAsync(workExperience);

        public async Task AddRangeAsync(List<WorkExperience> workExperiences)
            => await CreateManyAsync(workExperiences);

        public async Task EditAsync(Expression<Func<WorkExperience, bool>> expression
            , WorkExperience workExperience)
            => await UpdateAsync(expression, workExperience);

        public async Task DeleteAsync(Expression<Func<WorkExperience, bool>> expression)
            => await RemoveAsync(expression);

        public async Task DeleteRangeAsync(Expression<Func<WorkExperience, bool>> expression,
            CancellationToken token)
            => await RemoveManyAsync(expression, token);

        public async Task<bool> HasAnyAsync(Expression<Func<WorkExperience, bool>> expression)
            => await ExistsAsync(expression);

        public async Task<long> CountAllAsync(Expression<Func<WorkExperience, bool>> expression) =>
            await CountAsync(expression);
    }
}
