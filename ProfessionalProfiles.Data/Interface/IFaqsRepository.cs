using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IFaqsRepository
    {
        Task AddAsync(Faqs Faqs);
        Task AddRangeAsync(List<Faqs> Faqs);
        Task<long> CountFaqsAsync(Expression<Func<Faqs, bool>> expression);
        Task DeleteAsync(Expression<Func<Faqs, bool>> expression);
        Task EditAsync(Expression<Func<Faqs, bool>> expression, Faqs entity);
        IQueryable<Faqs> FindAsQueryable(Expression<Func<Faqs, bool>> expression);
        Task<Faqs?> FindOneAsync(Expression<Func<Faqs, bool>> expression);
        Task<bool> HasAnyAsync(Expression<Func<Faqs, bool>> expression);
    }
}
