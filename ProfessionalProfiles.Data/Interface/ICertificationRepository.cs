using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface ICertificationRepository
    {
        Task AddAsync(Certification certification);
        Task AddRangeAsync(List<Certification> certifications);
        Task<long> CountAllAsync(Expression<Func<Certification, bool>> expression);
        Task DeleteAsync(Expression<Func<Certification, bool>> expression);
        Task DeleteRangeAsync(Expression<Func<Certification, bool>> expression, CancellationToken token);
        Task EditAsync(Expression<Func<Certification, bool>> expression, Certification certification);
        IQueryable<Certification> FindAsQueryable(Expression<Func<Certification, bool>> expression);
        Task<Certification?> FindAsync(Expression<Func<Certification, bool>> expression);
        Task<List<Certification>> FindRangeAsync(Expression<Func<Certification, bool>> expression);
        Task<bool> HasAnyAsync(Expression<Func<Certification, bool>> expression);
    }
}
