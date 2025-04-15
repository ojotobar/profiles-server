using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Interface
{
    public interface IAuditRepository
    {
        Task AddAsync(AuditLog log);
        Task AddManyAsync(List<AuditLog> logs);
        IQueryable<AuditLog> AsQueryable(Expression<Func<AuditLog, bool>> expression);
    }
}
