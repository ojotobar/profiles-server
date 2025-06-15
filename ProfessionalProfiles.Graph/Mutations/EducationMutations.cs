using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.List;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Common;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.Educations;
using ProfessionalProfiles.Graph.Extensions;
using ProfessionalProfiles.Graph.Validations;

namespace ProfessionalProfiles.Graph.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class EducationMutations
    {
        /// <summary>
        /// Add education records
        /// </summary>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> AddEducationAsync(EducationInput input,
            IRepositoryManager repository, [Service] UserManager<Professional> userManager)
        {
            var validator = new EducationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new Payload(message);
            }

            var loggedInUserId = repository.User.GetLoggedInUserId().ToGuid();
            if (loggedInUserId.IsEmpty())
            {
                return new Payload("Access denied");
            }

            var user = await userManager.FindByIdAsync(loggedInUserId.ToString());
            if (user == null)
            {
                return new Payload("User not found");
            }

            var education = EducationDto.CreateMap(loggedInUserId, input);
            await repository.Education.AddAsync(education);
            return new Payload("Education record added successfully", true);
        }

        /// <summary>
        /// Update education records
        /// </summary>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UpdateEducationAsync(Guid id, EducationInput input,
            IRepositoryManager repository, [Service] UserManager<Professional> userManager)
        {
            var validator = new EducationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new Payload(message);
            }

            var existingRecord = await repository.Education.FindOneAsync(e => e.Id.Equals(id) && !e.IsDeprecated);
            if (existingRecord == null)
            {
                return new Payload("Record not found");
            }

            existingRecord = EducationDto.CreateMap(existingRecord, input);
            await repository.Education.EditAsync(e => e.Id.Equals(existingRecord.Id), existingRecord);
            return new Payload("Education record updated successfully", true);
        }

        /// <summary>
        /// Deprecates education records
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> DeleteEducationAsync(Guid id, [Service] IRepositoryManager repository,
            [Service] UserManager<Professional> userManager)
        {
            var userId = repository.User.GetLoggedInUserId();
            var isValidUser = await userId.ValidateLoggedinUser(userManager);
            if (!isValidUser.IsSuccessful || isValidUser.User == null)
            {
                return new Payload(isValidUser.Message);
            }

            var existingRecord = await repository.Education.FindOneAsync(e => !e.IsDeprecated && e.Id.Equals(id));
            if (existingRecord == null)
            {
                return new Payload("Record not found");
            }

            existingRecord.IsDeprecated = true;
            existingRecord.UpdatedOn = DateTime.UtcNow;
            await repository.Education.EditAsync(e => e.Id.Equals(existingRecord.Id), existingRecord);

            var user = isValidUser.User;
            var canGenrateKey = await repository.CanGenerateApiKey(user.EmailConfirmed, user.Location != null, !string.IsNullOrWhiteSpace(user.ProfilePicture),
                !string.IsNullOrWhiteSpace(user.ResumeLink), user.SocialMedia.IsNotNullOrEmpty());
            if (!canGenrateKey.CanGenerate)
            {
                user.KeyMarker = default;
                await userManager.UpdateAsync(user);
            }

            return new Payload("Education record deleted successfully", true);
        }
    }
}
