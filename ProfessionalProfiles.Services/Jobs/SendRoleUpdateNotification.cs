using CSharpTypes.Extensions.Enumeration;
using Microsoft.Extensions.Logging;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Services.Interfaces;
using Quartz;

namespace ProfessionalProfiles.Services.Jobs
{
    public class SendRoleUpdateNotification : IJob
    {
        private readonly IServiceManager service;
        private readonly ILogger<SendRoleUpdateNotification> logger;

        public SendRoleUpdateNotification(IServiceManager service, ILogger<SendRoleUpdateNotification> logger)
        {
            this.service = service;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var email = context.MergedJobDataMap.GetString("Email");
            var name = context.MergedJobDataMap.GetString("Name");
            var role = context.MergedJobDataMap.GetString("Role");
            var origin = context.MergedJobDataMap.GetString("Origin");

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(name) &&
                !string.IsNullOrEmpty(role) && !string.IsNullOrEmpty(origin))
            {
                var sent = await service.Email.SendRoleChangeEmail(origin, email, name, role);
                if (sent)
                {

                    logger.LogInformation($"Email successfully sent to {email}");
                }
                else
                {
                    logger.LogError($"Email failed to send to {email}");
                }
            }
            else
            {
                this.logger.LogError($"One or more parameters are is null. Email: {email}, Name: {name}, Origin: {origin}, Role: {role}");
            }
        }
    }
}
