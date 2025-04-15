using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class AuditRepository(MongoDbSettings dbSettings) 
        : Repository<AuditLog>(dbSettings), IAuditRepository
    {
        public async Task AddAsync(AuditLog log) =>
            await CreateAsync(log);

        public async Task AddManyAsync(List<AuditLog> logs) =>
            await CreateManyAsync(logs);

        public IQueryable<AuditLog> AsQueryable(Expression<Func<AuditLog, bool>> expression) =>
            GetAsQueryable(expression); 
    }
}
