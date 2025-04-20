using CSharpTypes.Extensions.Enumeration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using Quartz;

namespace ProfessionalProfiles.Services.Jobs
{
    public class AuditLogJob : IJob
    {
        private readonly UserManager<Professional> userManager;
        private readonly IRepositoryManager repository;
        private readonly ILogger<AuditLogJob> logger;

        public AuditLogJob(UserManager<Professional> userManager, 
            IRepositoryManager repository, ILogger<AuditLogJob> logger)
        {
            this.userManager = userManager;
            this.repository = repository;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var userId = context.MergedJobDataMap.GetString("UserId");
            var actionId = context.MergedJobDataMap.GetInt("ActionId");
            var action = context.MergedJobDataMap.GetString("Action");
            var ip = context.MergedJobDataMap.GetString("Ip");
            var platform = context.MergedJobDataMap.GetString("Platform");

            if (userId != null && action != null && actionId.TryParseValue<EAction>(out var actionEnum) && 
                ip != null && platform != null)
            {
                var user = await userManager.FindByIdAsync(userId);
                if(user !=  null)
                {
                    var audit = new AuditLog
                    {
                        UserId = user.Id.ToString(),
                        Action = action,
                        ActionId = actionEnum,
                        IPAddress = ip,
                        Platform = platform,
                    };

                    await repository.Audit.AddAsync(audit);
                    logger.LogInformation($"Audit log successfully saved: {JsonConvert.SerializeObject(audit)}");
                }
                else
                {
                    logger.LogError($"No user found with the Id: {userId}");
                }
            }
            else
            {
                this.logger.LogError($"One or more parameters are is null. UserId: {userId}, Action: {action}, IP: {ip}, Platform: {platform}");
            }
        }
    }
}
