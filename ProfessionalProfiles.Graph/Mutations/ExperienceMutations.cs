using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Common;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.Experiences;
using ProfessionalProfiles.Graph.Extensions;
using ProfessionalProfiles.Graph.Validations;

namespace ProfessionalProfiles.Graph.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class ExperienceMutations
    {
        /// <summary>
        /// Add User Work Experiences
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> AddExperiencesAsync(List<ExperienceInput> inputs,
            IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
            }

            foreach (var input in inputs)
            {
                var validationResult = new ExperienceInputValidator().Validate(input);
                if (!validationResult.IsValid)
                {
                    var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                    return new Payload(message);
                }
            }

            var experiences = inputs.Initialize(userId);
            await repository.WorkExperience.AddRangeAsync(experiences);
            return new Payload("Experiences added successfully", true);
        }

        /// <summary>
        /// Update Work Experience
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UpdateExperienceAsync(Guid id, ExperienceInput input,
            IRepositoryManager repository)
        {
            var validationResult = new ExperienceInputValidator().Validate(input);
            if (!validationResult.IsValid)
            {
                var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new Payload(message);
            }

            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
            }

            var experience = await repository.WorkExperience.FindAsync(p => p.UserId.Equals(userId) && p.Id.Equals(id));
            if (experience.IsNull())
            {
                return new Payload("Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(experience!.UserId)))
            {
                return new Payload("You're not authorized to perform this action!");
            }

            experience = input.Map(experience!);
            await repository.WorkExperience.EditAsync(c => c.Id.Equals(id), experience);
            return new Payload("Experience updated successfully", true);
        }

        /// <summary>
        /// Deletes certification records
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> DeleteExperienceAsync(Guid id, [Service] IRepositoryManager repository,
            [Service] UserManager<Professional> userManager)
        {
            var userId = repository.User.GetLoggedInUserId();
            var isValidUser = await userId.ValidateLoggedinUser(userManager);
            if (!isValidUser.IsSuccessful || isValidUser.User == null)
            {
                return new Payload(isValidUser.Message);
            }

            var experience = await repository.WorkExperience.FindAsync(c => c.Id.Equals(id));
            if (experience.IsNull())
            {
                return new Payload("Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(experience!.UserId)))
            {
                return new Payload("You're not authorized to perform this action!");
            }

            await repository.WorkExperience.DeleteAsync(c => c.Id.Equals(id));

            var user = isValidUser.User;
            var canGenrateKey = await repository.CanGenerateApiKey(user.EmailConfirmed, user.Location != null, !string.IsNullOrWhiteSpace(user.ProfilePicture),
                !string.IsNullOrWhiteSpace(user.ResumeLink), user.SocialMedia.IsNotNullOrEmpty());
            if (!canGenrateKey.CanGenerate)
            {
                user.KeyMarker = default;
                await userManager.UpdateAsync(user);
            }

            return new Payload("Experience deleted successfully", true);
        }
    }
}