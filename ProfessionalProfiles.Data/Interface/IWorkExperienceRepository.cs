using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IWorkExperienceRepository
    {
        Task AddAsync(WorkExperience workExperience);
        Task AddRangeAsync(List<WorkExperience> workExperiences);
        Task DeleteAsync(Expression<Func<WorkExperience, bool>> expression);
        Task DeleteRangeAsync(Expression<Func<WorkExperience, bool>> expression, CancellationToken token);
        Task EditAsync(Expression<Func<WorkExperience, bool>> expression, WorkExperience workExperience);
        IQueryable<WorkExperience> FindAsQueryable(Expression<Func<WorkExperience, bool>> expression);
        Task<WorkExperience?> FindAsync(Expression<Func<WorkExperience, bool>> expression);
        Task<List<WorkExperience>> FindRangeAsync(Expression<Func<WorkExperience, bool>> expression);
        Task<bool> HasAnyAsync(Expression<Func<WorkExperience, bool>> expression);
    }
}
