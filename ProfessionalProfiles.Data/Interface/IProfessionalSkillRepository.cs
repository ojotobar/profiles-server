using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IProfessionalSkillRepository
    {
        Task AddAsync(ProfessionalSkill professionalSkill);
        Task AddRangeAsync(List<ProfessionalSkill> professionalSkills);
        Task DeleteAsync(Expression<Func<ProfessionalSkill, bool>> expression);
        Task DeleteRangeAsync(Expression<Func<ProfessionalSkill, bool>> expression, CancellationToken token);
        Task EditAsync(Expression<Func<ProfessionalSkill, bool>> expression, ProfessionalSkill professionalSkill);
        IQueryable<ProfessionalSkill> FindAsQueryable(Expression<Func<ProfessionalSkill, bool>> expression);
        Task<ProfessionalSkill?> FindAsync(Expression<Func<ProfessionalSkill, bool>> expression);
        Task<List<ProfessionalSkill>> FindRangeAsync(Expression<Func<ProfessionalSkill, bool>> expression);
        Task<bool> HasAnyAsync(Expression<Func<ProfessionalSkill, bool>> expression);
    }
}
