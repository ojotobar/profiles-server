using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class PortfolioVersionRespository(MongoDbSettings dbSettings) 
        : Repository<PortfolioVersion>(dbSettings), IPortfolioVersionRespository
    {
        public async Task<PortfolioVersion?> FindAsync(Expression<Func<PortfolioVersion, bool>> expression)
            => await GetAsync(expression);

        public IQueryable<PortfolioVersion> FindAsQueryable(Expression<Func<PortfolioVersion, bool>> expression)
            => GetAsQueryable(expression);

        public async Task<List<PortfolioVersion>> FindRangeAsync(Expression<Func<PortfolioVersion, bool>> expression)
            => await GetManyAsync(expression);

        public async Task AddAsync(PortfolioVersion skill)
            => await CreateAsync(skill);

        public async Task AddRangeAsync(List<PortfolioVersion> skills)
            => await CreateManyAsync(skills);

        public async Task EditAsync(Expression<Func<PortfolioVersion, bool>> expression
            , PortfolioVersion skill)
            => await UpdateAsync(expression, skill);

        public async Task DeleteAsync(Expression<Func<PortfolioVersion, bool>> expression)
            => await RemoveAsync(expression);

        public async Task DeleteRangeAsync(Expression<Func<PortfolioVersion, bool>> expression,
            CancellationToken token)
            => await RemoveManyAsync(expression, token);

        public async Task<bool> HasAnyAsync(Expression<Func<PortfolioVersion, bool>> expression)
            => await ExistsAsync(expression);

        public async Task<long> CountAllAsync(Expression<Func<PortfolioVersion, bool>> expression) =>
            await CountAsync(expression);
    }
}
