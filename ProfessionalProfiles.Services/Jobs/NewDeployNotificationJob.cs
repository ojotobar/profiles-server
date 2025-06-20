using DRY.MailJetClient.Library;
using Microsoft.Extensions.Logging;
using Quartz;
using System.Text.Json;

namespace ProfessionalProfiles.Services.Jobs
{
    public class NewDeployNotificationJob : IJob
    {
        private readonly IMailjetClientService mailJet;
        private readonly ILogger<NewDeployNotificationJob> logger;

        public NewDeployNotificationJob(IMailjetClientService mailJet, ILogger<NewDeployNotificationJob> logger)
        {
            this.mailJet = mailJet;
            this.logger = logger;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var template = context.MergedJobDataMap.GetString("Message");
            var dictJson = context.MergedJobDataMap.GetString("EmailAndNameMap");
            var subject = context.MergedJobDataMap.GetString("Subject");

            if(!string.IsNullOrWhiteSpace(dictJson) && !string.IsNullOrWhiteSpace(template) && 
                !string.IsNullOrWhiteSpace(subject))
            {
                var dicts = JsonSerializer.Deserialize<Dictionary<string, string>>(dictJson);
                if(dicts != null && dicts.Count > 0)
                {
                    foreach (var nameEmail in dicts)
                    {
                        if(!string.IsNullOrWhiteSpace(nameEmail.Key) && !string.IsNullOrWhiteSpace(nameEmail.Value))
                        {
                            logger.LogInformation($"Sending message to {nameEmail.Value}");

                            var message = template.Replace("{{userName}}", nameEmail.Value);
                            var sent = await mailJet.SendAsync(nameEmail.Key, message, subject);
                            if (sent)
                            {
                                logger.LogInformation($"Message successfully sent to {nameEmail.Value}");
                            }
                            else
                            {
                                logger.LogError($"An error occurred while sending the message to {nameEmail.Value}");
                            }
                        }
                        else
                        {
                            logger.LogWarning($"Email not sent. User's name or email has no value.");
                        }
                    }
                }
                else
                {
                    logger.LogWarning($"The users Name and Email dictionary is either null or contains no item.");
                }
            }
            else
            {
                logger.LogWarning($"The users Json string, message or subject is null.");
            }
        }
    }
}
