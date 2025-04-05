using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class CertificationRepository(MongoDbSettings settings) 
        : Repository<Certification>(settings), ICertificationRepository
    {
        public async Task<Certification?> FindAsync(Expression<Func<Certification, bool>> expression)
            => await GetAsync(expression);

        public IQueryable<Certification> FindAsQueryable(Expression<Func<Certification, bool>> expression)
            => GetAsQueryable(expression);

        public async Task<List<Certification>> FindRangeAsync(Expression<Func<Certification, bool>> expression)
            => await GetManyAsync(expression);

        public async Task AddAsync(Certification certification)
            => await CreateAsync(certification);

        public async Task AddRangeAsync(List<Certification> certifications)
            => await CreateManyAsync(certifications);

        public async Task EditAsync(Expression<Func<Certification, bool>> expression
            , Certification certification)
            => await UpdateAsync(expression, certification);

        public async Task DeleteAsync(Expression<Func<Certification, bool>> expression)
            => await RemoveAsync(expression);

        public async Task DeleteRangeAsync(Expression<Func<Certification, bool>> expression,
            CancellationToken token)
            => await RemoveManyAsync(expression, token);

        public async Task<bool> HasAnyAsync(Expression<Func<Certification, bool>> expression)
            => await ExistsAsync(expression);

        public async Task<long> CountAllAsync(Expression<Func<Certification, bool>> expression) =>
            await CountAsync(expression);
    }
}
