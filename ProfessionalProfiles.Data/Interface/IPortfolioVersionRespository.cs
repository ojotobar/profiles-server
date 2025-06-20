using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IPortfolioVersionRespository
    {
        Task AddAsync(PortfolioVersion skill);
        Task AddRangeAsync(List<PortfolioVersion> skills);
        Task<long> CountAllAsync(Expression<Func<PortfolioVersion, bool>> expression);
        Task DeleteAsync(Expression<Func<PortfolioVersion, bool>> expression);
        Task DeleteRangeAsync(Expression<Func<PortfolioVersion, bool>> expression, CancellationToken token);
        Task EditAsync(Expression<Func<PortfolioVersion, bool>> expression, PortfolioVersion skill);
        IQueryable<PortfolioVersion> FindAsQueryable(Expression<Func<PortfolioVersion, bool>> expression);
        Task<PortfolioVersion?> FindAsync(Expression<Func<PortfolioVersion, bool>> expression);
        Task<List<PortfolioVersion>> FindRangeAsync(Expression<Func<PortfolioVersion, bool>> expression);
        Task<bool> HasAnyAsync(Expression<Func<PortfolioVersion, bool>> expression);
    }
}
