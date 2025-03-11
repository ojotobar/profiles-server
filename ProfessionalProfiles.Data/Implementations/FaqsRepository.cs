using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class FaqsRepository(MongoDbSettings dbSettings) 
        : Repository<Faqs>(dbSettings), IFaqsRepository
    {
        public async Task AddAsync(Faqs Faqs) =>
            await CreateAsync(Faqs);

        public async Task AddRangeAsync(List<Faqs> Faqs) =>
            await CreateManyAsync(Faqs);

        public async Task EditAsync(Expression<Func<Faqs, bool>> expression, Faqs entity) =>
            await UpdateAsync(expression, entity);

        public async Task<Faqs?> FindOneAsync(Expression<Func<Faqs, bool>> expression) =>
            await GetAsync(expression);

        public IQueryable<Faqs> FindAsQueryable(Expression<Func<Faqs, bool>> expression) =>
            GetAsQueryable(expression);

        public async Task DeleteAsync(Expression<Func<Faqs, bool>> expression)
            => await RemoveAsync(expression);

        public async Task<long> CountFaqsAsync(Expression<Func<Faqs, bool>> expression)
            => await CountAsync(expression);

        public async Task<bool> HasAnyAsync(Expression<Func<Faqs, bool>> expression)
            => await ExistsAsync(expression);
    }
}
