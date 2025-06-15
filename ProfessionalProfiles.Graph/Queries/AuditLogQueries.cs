using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Dto;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class AuditLogQueries
    {
        /// <summary>
        /// Gets paginated audit logs
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        [UseOffsetPaging(IncludeTotalCount = true)]
        [Authorize(Roles = ["Admin"])]
        public IQueryable<AuditLog> GetAuditLogs([Service] IRepositoryManager repository,
            UserManager<Professional> userManager,
            AuditLogFilterInput? search)
        {
            return repository.Audit
                .AsQueryable(al => !al.IsDeprecated)
                .Map(userManager)
                .Filter(search)
                .OrderByDescending(a => a.CreatedOn);
        }
    }
}