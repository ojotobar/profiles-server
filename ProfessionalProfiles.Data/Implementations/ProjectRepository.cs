using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class ProjectRepository(MongoDbSettings settings)
        : Repository<Project>(settings), IProjectRepository
    {
        public async Task<Project?> FindAsync(Expression<Func<Project, bool>> expression)
            => await GetAsync(expression);

        public IQueryable<Project> FindAsQueryable(Expression<Func<Project, bool>> expression)
            => GetAsQueryable(expression);

        public async Task<List<Project>> FindRangeAsync(Expression<Func<Project, bool>> expression)
            => await GetManyAsync(expression);

        public async Task AddAsync(Project project)
            => await CreateAsync(project);

        public async Task AddRangeAsync(List<Project> projects)
            => await CreateManyAsync(projects);

        public async Task EditAsync(Expression<Func<Project, bool>> expression
            , Project project)
            => await UpdateAsync(expression, project);

        public async Task DeleteAsync(Expression<Func<Project, bool>> expression)
            => await RemoveAsync(expression);

        public async Task DeleteRangeAsync(Expression<Func<Project, bool>> expression,
            CancellationToken token)
            => await RemoveManyAsync(expression, token);

        public async Task<bool> HasAnyAsync(Expression<Func<Project, bool>> expression)
            => await ExistsAsync(expression);
    }
}
