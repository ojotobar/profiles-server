using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IProfessionalSummaryRepository
    {
        Task AddAsync(ProfessionalSummary professionalSummary);
        Task DeleteAsync(Expression<Func<ProfessionalSummary, bool>> expression);
        Task EditAsync(Expression<Func<ProfessionalSummary, bool>> expression, ProfessionalSummary professionalSummary);
        Task<ProfessionalSummary?> FindAsync(Expression<Func<ProfessionalSummary, bool>> expression);
        Task<bool> HasAsync(Expression<Func<ProfessionalSummary, bool>> expression);
    }
}
