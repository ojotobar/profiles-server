using CSharpTypes.Extensions.Enumeration;
using Microsoft.Extensions.Logging;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Services.Interfaces;
using Quartz;

namespace ProfessionalProfiles.Services.Jobs
{
    public class StatusChangeNotification : IJob
    {
        private readonly IServiceManager service;
        private readonly ILogger<StatusChangeNotification> logger;

        public StatusChangeNotification(IServiceManager service, ILogger<StatusChangeNotification> logger)
        {
            this.service = service;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var email = context.MergedJobDataMap.GetString("Email");
            var name = context.MergedJobDataMap.GetString("Name");
            var statusInt = context.MergedJobDataMap.GetInt("Status");
            var origin = context.MergedJobDataMap.GetString("Origin");

            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(name) && 
                statusInt.TryParseValue<EStatus>(out var status) && !string.IsNullOrEmpty(origin))
            {
                var sent = await service.Email.SendStatusChangeEmail(origin, email, name, status);
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
                this.logger.LogError($"One or more parameters are is null. Email: {email}, Name: {name}, Origin: {origin}, Status: {statusInt}");
            }
        }
    }
}
