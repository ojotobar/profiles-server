using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Common;
using ProfessionalProfiles.Graph.General;
using ProfessionalProfiles.Services.Interfaces;
using ProfessionalProfiles.Services.Jobs;

namespace ProfessionalProfiles.Graph.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class PortfolioVersionMutations
    {
        /// <summary>
        /// Notify users of new updates to the portfolio client they are using.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> SendNewVersionNotificationAsync(NewVersionNotificationInput input,
            [Service] UserManager<Professional> userManager, [Service] IServiceManager service, 
            [Service] IRepositoryManager repository, ILogger<PortfolioVersionMutations> logger)
        {
            ArgumentNullException.ThrowIfNull(input);
            if (string.IsNullOrEmpty(input.Version) ||
                string.IsNullOrEmpty(input.Tag) || string.IsNullOrEmpty(input.Env))
            {
                return new Payload("All the input fields are required.");
            }

            var existingTag = await repository.PortfolioVersion.FindAsync(v => v.Name.Equals(input.Tag) && !v.IsDeprecated);
            if (existingTag == null)
            {
                var newVersion = new PortfolioVersion
                {
                    Name = input.Tag,
                    LatestVersion = input.Version,
                    IsPremium = input.IsPremium
                };

                await repository.PortfolioVersion.AddAsync(newVersion);
                logger.LogInformation($"New Portfolio Version Added. \nTag: {input.Tag} \nVersion: {input.Version}");
            }
            else
            {
                existingTag.OldVersion = existingTag.LatestVersion;
                existingTag.LatestVersion = input.Version;
                existingTag.UpdatedOn = DateTime.UtcNow;

                await repository.PortfolioVersion.EditAsync(v => v.Id.Equals(existingTag.Id), existingTag);
                logger.LogInformation($"Portfolio Version Updated. \nTag: {input.Tag}. \nOld Version: {existingTag.OldVersion}. \nLatest Version: {input.Version}.");
            }

            var users = await Task.Run(() => userManager.Users
                .Where(u => u.Status == EStatus.Active && !u.IsDeprecated
                    && u.LatestUsedClientTag.Equals(input.Tag) && u.KeyMarker != default)
                .ToList());

            if(users != null && users.Count > 0)
            {
                await service.Email.SendDeployNotificationEmailAsync(users, input.Tag);
                return new Payload($"New update alert for {users.Count} users started.", true);
            }
            else
            {
                return new Payload($"No user found using the {input.Tag} tag yet.", true);
            }
        }
    }
}
