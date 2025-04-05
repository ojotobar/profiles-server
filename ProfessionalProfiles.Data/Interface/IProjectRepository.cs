using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IProjectRepository
    {
        Task AddAsync(Project project);
        Task AddRangeAsync(List<Project> projects);
        Task<long> CountAllAsync(Expression<Func<Project, bool>> expression);
        Task DeleteAsync(Expression<Func<Project, bool>> expression);
        Task DeleteRangeAsync(Expression<Func<Project, bool>> expression, CancellationToken token);
        Task EditAsync(Expression<Func<Project, bool>> expression, Project project);
        IQueryable<Project> FindAsQueryable(Expression<Func<Project, bool>> expression);
        Task<Project?> FindAsync(Expression<Func<Project, bool>> expression);
        Task<List<Project>> FindRangeAsync(Expression<Func<Project, bool>> expression);
        Task<bool> HasAnyAsync(Expression<Func<Project, bool>> expression);
    }
}
