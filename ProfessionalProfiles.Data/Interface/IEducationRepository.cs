using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IEducationRepository
    {
        Task AddAsync(Education education);
        Task AddManyAsync(List<Education> educations);
        Task EditAsync(Expression<Func<Education, bool>> expression, Education entity);
        IQueryable<Education> FindAsQueryable(Expression<Func<Education, bool>> expression);
        Task<Education?> FindOneAsync(Expression<Func<Education, bool>> expression);
    }
}
