using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.CareerSummaries;
using ProfessionalProfiles.Graph.Common;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.Extensions;

namespace ProfessionalProfiles.Graph.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class ProfessionalSummaryMutations
    {
        /// <summary>
        /// Add User Professional summary
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> AddProfessionalSummaryAsync(ProfessionalSummaryInput input,
            IRepositoryManager repository)
        {
            if (input.Summary.IsNullOrEmpty())
            {
                return new Payload("Professional summary field is required.");
            }

            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
            }

            var existingRecord = await repository.Summary.FindAsync(s => s.UserId.Equals(userId));
            if (existingRecord == null)
            {
                var summary = input.Initialize(userId);
                await repository.Summary.AddAsync(summary);
                return new Payload("Professional Summary added successfully", true);
            }
            else //A user may only have one record. Update if already exists.
            {
                existingRecord.Summary = input.Summary;
                existingRecord.UpdatedOn = DateTime.UtcNow;
                await repository.Summary.EditAsync(s => s.Id.Equals(existingRecord.Id), existingRecord);
                return new Payload("Professional Summary updated successfully", true);
            }
        }

        /// <summary>
        /// Updates professional summary records
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UpdateCareerSummaryAsync(Guid id, ProfessionalSummaryInput input,
            IRepositoryManager repository)
        {
            if (input.Summary.IsNullOrEmpty())
            {
                return new Payload("Professional summary field is required.");
            }

            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
            }

            var summary = await repository.Summary.FindAsync(s => s.Id.Equals(id) && s.UserId.Equals(userId));
            if (summary.IsNull())
            {
                return new Payload("Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(summary!.UserId)))
            {
                return new Payload("You're not authorized to perform this action!");
            }

            summary = input.Map(summary!);
            await repository.Summary.EditAsync(c => c.Id.Equals(id), summary);
            return new Payload("Professional summary updated successfully", true);
        }

        /// <summary>
        /// Deletes Professional Summary records
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> DeleteCareerSummaryAsync(Guid id, [Service] IRepositoryManager repository,
            [Service] UserManager<Professional> userManager)
        {
            var userId = repository.User.GetLoggedInUserId();
            var isValidUser = await userId.ValidateLoggedinUser(userManager);
            if (!isValidUser.IsSuccessful || isValidUser.User == null)
            {
                return new Payload(isValidUser.Message);
            }

            var summary = await repository.Summary.FindAsync(c => c.Id.Equals(id));
            if (summary.IsNull())
            {
                return new Payload("Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(summary!.UserId)))
            {
                return new Payload("You're not authorized to perform this action!");
            }

            await repository.Summary.DeleteAsync(c => c.Id.Equals(id));

            var user = isValidUser.User;
            var canGenrateKey = await repository.CanGenerateApiKey(user.EmailConfirmed, user.Location != null, !string.IsNullOrWhiteSpace(user.ProfilePicture),
                !string.IsNullOrWhiteSpace(user.ResumeLink), user.SocialMedia.IsNotNullOrEmpty());
            if (!canGenrateKey.CanGenerate)
            {
                user.KeyMarker = default;
                await userManager.UpdateAsync(user);
            }

            return new Payload("Professional Summary record deleted successfully", true);
        }
    }
}