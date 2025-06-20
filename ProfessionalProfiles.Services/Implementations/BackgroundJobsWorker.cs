using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Services.Interfaces;
using ProfessionalProfiles.Services.Jobs;
using Quartz;
using System.Text.Json;

namespace ProfessionalProfiles.Services.Implementations
{
    public class BackgroundJobsWorker
    {
        private readonly ISchedulerFactory _schedulerFactory;

        public BackgroundJobsWorker(ISchedulerFactory schedulerFactory)
        {
            _schedulerFactory = schedulerFactory;
        }

        public async Task LogAuditAsync(AuditLog auditLog)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var job = JobBuilder.Create<AuditLogJob>()
                .UsingJobData("UserId", auditLog.UserId)
                .UsingJobData("Action", auditLog.Action)
                .UsingJobData("ActionId", (int)auditLog.ActionId)
                .UsingJobData("Ip", auditLog.IPAddress)
                .UsingJobData("Platform", auditLog.Platform)
                .WithIdentity(Guid.NewGuid().ToString())
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            await scheduler.Start();
        }

        public async Task SendStatusChangeEmailAsync(string origin, string email, string name, EStatus status)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var job = JobBuilder.Create<StatusChangeNotification>()
                .UsingJobData("Email", email)
                .UsingJobData("Name", name)
                .UsingJobData("Status", (int)status)
                .UsingJobData("Origin", origin)
                .WithIdentity(Guid.NewGuid().ToString())
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            await scheduler.Start();
        }

        public async Task SendRoleUpdateEmailAsync(string origin, string email, string name, string role)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var job = JobBuilder.Create<SendRoleUpdateNotification>()
                .UsingJobData("Email", email)
                .UsingJobData("Name", name)
                .UsingJobData("Role", role)
                .UsingJobData("Origin", origin)
                .WithIdentity(Guid.NewGuid().ToString())
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            await scheduler.Start();
        }

        public async Task CleanUpFirebaseStorage(bool deleteAll = false)
        {
            var scheduler = await _schedulerFactory.GetScheduler();

            var job = JobBuilder.Create<FirebaseFilesCleanup>()
                .UsingJobData("DeleteAll", deleteAll)
                .WithIdentity(Guid.NewGuid().ToString())
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            await scheduler.Start();
        }

        public async Task SendNewDeploymentNofication(Dictionary<string, string> nameAndEmailMap, string html, string subject)
        {
            var scheduler = await _schedulerFactory.GetScheduler();
            var dictJson = JsonSerializer.Serialize(nameAndEmailMap);

            var job = JobBuilder.Create<NewDeployNotificationJob>()
                .UsingJobData("Message", html)
                .UsingJobData("EmailAndNameMap", dictJson)
                .UsingJobData("Subject", subject)
                .WithIdentity(Guid.NewGuid().ToString())
                .Build();

            var trigger = TriggerBuilder.Create()
                .StartNow()
                .Build();

            await scheduler.ScheduleJob(job, trigger);
            await scheduler.Start();
        }
    }
}
