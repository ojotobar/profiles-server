using CSharpTypes.Extensions.Enumeration;
using HotChocolate.Authorization;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Common;
using ProfessionalProfiles.Services.Implementations;

namespace ProfessionalProfiles.Graph.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class FaqsMutations
    {
        /// <summary>
        /// Add FAQs record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> AddFaqAsync(FaqsInput input, [Service] IRepositoryManager repository,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog)
        {
            if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Content))
            {
                return new Payload("The Title and the Content are required.");
            }

            await repository.Faqs.AddAsync(new Faqs
            {
                Title = input.Title,
                Content = input.Content
            });

            if (auditLog != null)
            {
                auditLog.ActionId = EAction.AuditLogs;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), "Added", $"{input.Title}");
                auditLog.UserId = repository.User.GetLoggedInUserId();
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload("FAQs record successfully added", true);
        }

        /// <summary>
        /// Update FAQs record
        /// </summary>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> UpdateFaqAsync(Guid id, FaqsInput input, [Service] IRepositoryManager repository,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog)
        {
            if (string.IsNullOrWhiteSpace(input.Title) || string.IsNullOrWhiteSpace(input.Content))
            {
                return new Payload("The Title and the Content are required.");
            }

            var record = await repository.Faqs.FindOneAsync(f => f.Id.Equals(id));
            if (record == null)
            {
                return new Payload("No FAQs record found with the Id provided.");
            }

            var prevTitle = record.Title;
            record.Title = input.Title;
            record.Content = input.Content;
            record.UpdatedOn = DateTime.UtcNow;

            await repository.Faqs.EditAsync(f => f.Id.Equals(id), record);
            if (auditLog != null)
            {
                auditLog.ActionId = EAction.AuditLogs;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), "Updated", $"{prevTitle} to {record.Title}");
                auditLog.UserId = repository.User.GetLoggedInUserId();
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload("FAQs record successfully updated", true);
        }

        /// <summary>
        /// Deletes FAQs record
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <param name="auditLogger"></param>
        /// <param name="auditLog"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> DeleteFaqAsync(Guid id, [Service] IRepositoryManager repository,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog)
        {
            var record = await repository.Faqs.FindOneAsync(f => f.Id.Equals(id));
            if (record == null)
            {
                return new Payload("No FAQs record found with the Id provided.");
            }

            await repository.Faqs.DeleteAsync(f => f.Id.Equals(id));
            if (auditLog != null)
            {
                auditLog.ActionId = EAction.AuditLogs;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), "Deleted", $"{record.Title}");
                auditLog.UserId = repository.User.GetLoggedInUserId();
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload("FAQs record successfully deleted", true);
        }
    }
}
