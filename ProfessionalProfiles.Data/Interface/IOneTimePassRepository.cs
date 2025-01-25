using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IOneTimePassRepository
    {
        Task AddAsync(OneTimePass oneTimePass);
        Task DeleteAsync(Expression<Func<OneTimePass, bool>> predicate);
        Task DeleteManyAsync(Expression<Func<OneTimePass, bool>> predicate, CancellationToken cancellation);
        Task EditAsync(Expression<Func<OneTimePass, bool>> expression, OneTimePass entity);
        Task<OneTimePass?> FindOneAsync(Expression<Func<OneTimePass, bool>> predicate);
    }
}
