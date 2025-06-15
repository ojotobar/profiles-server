using Microsoft.Extensions.Logging;
using ProfessionalProfiles.Services.Interfaces;
using Quartz;

namespace ProfessionalProfiles.Services.Jobs
{
    public class FirebaseFilesCleanup(IServiceManager service) : IJob
    {
        private readonly IServiceManager _service = service;

        public async Task Execute(IJobExecutionContext context)
        {
            var deleteAll = context.MergedJobDataMap.GetBoolean("DeleteAll");
            await _service.Firebase.CleanFolderAsync(deleteAll);
        }
    }
}
