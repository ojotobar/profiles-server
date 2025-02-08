using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class ProfessionalSummaryRepository(MongoDbSettings settings) 
        : Repository<ProfessionalSummary>(settings), IProfessionalSummaryRepository
    {
        public async Task<ProfessionalSummary?> FindAsync(Expression<Func<ProfessionalSummary, bool>> expression)
            => await GetAsync(expression);

        public async Task AddAsync(ProfessionalSummary professionalSummary)
            => await CreateAsync(professionalSummary);

        public async Task EditAsync(Expression<Func<ProfessionalSummary, bool>> expression
            , ProfessionalSummary professionalSummary)
            => await UpdateAsync(expression, professionalSummary);

        public async Task DeleteAsync(Expression<Func<ProfessionalSummary, bool>> expression)
            => await RemoveAsync(expression);

        public async Task<bool> HasAsync(Expression<Func<ProfessionalSummary, bool>> expression)
            => await ExistsAsync(expression);
    }
}
