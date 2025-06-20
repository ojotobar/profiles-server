using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.List;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Common;
using ProfessionalProfiles.Graph.Extensions;
using ProfessionalProfiles.Graph.Skills;
using ProfessionalProfiles.Graph.Validations;
using ProfessionalProfiles.Graph.Dto;

namespace ProfessionalProfiles.Graph.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class SkillsMutations
    {
        private const long MAXSKILLCOUNT = 20;

        /// <summary>
        /// Adds a list of user skills
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> AddSkillsAsync(List<SkillInput> inputs, IRepositoryManager repository)
        {
            var userId = await repository.User.GetLoggedInOrApiKeyUserId();
            if (userId.IsEmpty())
            {
                return new Payload("Access denied!!!");
            }

            if (!inputs.IsNotNullOrEmpty())
            {
                return new Payload("You must enter one or more skills");
            }

            var count = await repository.Skill.CountAllAsync(s => s.UserId.Equals(userId));
            if (count >= MAXSKILLCOUNT || MAXSKILLCOUNT < (count + inputs.Count))
            {
                return new Payload("You can only add a maximum of 20 skills. Please remove some and try again later");
            }

            var validator = new SkillInputValidator();
            foreach (var item in inputs)
            {
                var result = validator.Validate(item);
                if (!result.IsValid)
                {
                    var message = result.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                    return new Payload(message);
                }
            }

            var skillsToAdd = inputs.Map(userId);
            await repository.Skill.AddRangeAsync(skillsToAdd);
            return new Payload("Skills update successful", true);
        }

        /// <summary>
        /// Update user skills
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <param name="apiKey"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UpdateSkillAsync(Guid id, SkillInput input, IRepositoryManager repository)
        {
            var validator = new SkillInputValidator();
            var result = validator.Validate(input);
            if (!result.IsValid)
            {
                var message = result.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new Payload(message);
            }

            var skillToUpdate = await repository.Skill.FindAsync(s => s.Id.Equals(id) && !s.IsDeprecated);
            if (skillToUpdate == null)
            {
                return new Payload("No record found for the provided id");
            }

            input.Map(skillToUpdate);
            await repository.Skill.EditAsync(s => s.Id.Equals(skillToUpdate.Id), skillToUpdate);
            return new Payload("Skill update successful", true);
        }

        /// <summary>
        /// Deletes user skills
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> DeleteSkillAsync(Guid id, IRepositoryManager repository,
            [Service] UserManager<Professional> userManager)
        {
            var userId = repository.User.GetLoggedInUserId();
            var isValidUser = await userId.ValidateLoggedinUser(userManager);
            if (!isValidUser.IsSuccessful || isValidUser.User == null)
            {
                return new Payload(isValidUser.Message);
            }

            var skillToDelete = await repository.Skill.FindAsync(s => s.Id.Equals(id) && !s.IsDeprecated);
            if (skillToDelete == null)
            {
                return new Payload("No record found for the provided id");
            }

            await repository.Skill.DeleteAsync(s => s.Id.Equals(skillToDelete.Id));

            var user = isValidUser.User;
            var canGenrateKey = await repository.CanGenerateApiKey(user.EmailConfirmed, user.Location != null, !string.IsNullOrWhiteSpace(user.ProfilePicture),
                !string.IsNullOrWhiteSpace(user.ResumeLink), user.SocialMedia.IsNotNullOrEmpty());
            if (!canGenrateKey.CanGenerate)
            {
                user.KeyMarker = default;
                await userManager.UpdateAsync(user);
            }

            return new Payload("Skill deleted successful", true);
        }
    }
}
