using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface ISkillRepository
    {
        Task AddAsync(Skill skill);
        Task AddRangeAsync(List<Skill> skills);
        Task DeleteAsync(Expression<Func<Skill, bool>> expression);
        Task DeleteRangeAsync(Expression<Func<Skill, bool>> expression, CancellationToken token);
        Task EditAsync(Expression<Func<Skill, bool>> expression, Skill skill);
        IQueryable<Skill> FindAsQueryable(Expression<Func<Skill, bool>> expression);
        Task<Skill?> FindAsync(Expression<Func<Skill, bool>> expression);
        Task<List<Skill>> FindRangeAsync(Expression<Func<Skill, bool>> expression);
        Task<bool> HasAnyAsync(Expression<Func<Skill, bool>> expression);
    }
}
