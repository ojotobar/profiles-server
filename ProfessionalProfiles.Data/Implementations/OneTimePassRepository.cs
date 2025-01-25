using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class OneTimePassRepository : Repository<OneTimePass>, IOneTimePassRepository
    {
        public OneTimePassRepository(MongoDbSettings settings) : base(settings) { }
        
        public async Task AddAsync(OneTimePass oneTimePass) 
            => await CreateAsync(oneTimePass);

        public async Task<OneTimePass?> FindOneAsync(Expression<Func<OneTimePass, bool>> predicate) 
            => await GetAsync(predicate);

        public async Task EditAsync(Expression<Func<OneTimePass, bool>> expression, OneTimePass entity) 
            => await UpdateAsync(expression, entity);

        public async Task DeleteAsync(Expression<Func<OneTimePass, bool>> predicate)
            => await RemoveAsync(predicate);

        public async Task DeleteManyAsync(Expression<Func<OneTimePass, bool>> predicate, 
            CancellationToken cancellation)
            => await RemoveManyAsync(predicate, cancellation);
    }
}
