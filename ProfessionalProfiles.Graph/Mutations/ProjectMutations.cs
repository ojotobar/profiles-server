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
using ProfessionalProfiles.Graph.Extensions;
using ProfessionalProfiles.Graph.Projects;
using ProfessionalProfiles.Graph.Validations;

namespace ProfessionalProfiles.Graph.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class ProjectMutations
    {
        /// <summary>
        /// Add User Projects
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> AddProjectsAsync(List<ProjectInput> inputs,
            IRepositoryManager repository)
        {
            foreach (var input in inputs)
            {
                var validationResult = new ProjectInputValidator().Validate(input);
                if (!validationResult.IsValid)
                {
                    var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                    return new Payload(message);
                }
            }

            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
            }

            var projects = inputs.Initialize(userId);
            await repository.Project.AddRangeAsync(projects);
            return new Payload("Projects added successfully", true);
        }

        /// <summary>
        /// Updated Projects
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UpdateProjectAsync(Guid id, ProjectInput input,
            IRepositoryManager repository)
        {
            var validationResult = new ProjectInputValidator().Validate(input);
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

            var project = await repository.Project.FindAsync(p => p.UserId.Equals(userId) && p.Id.Equals(id));
            if (project.IsNull())
            {
                return new Payload("Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(project!.UserId)))
            {
                return new Payload("You're not authorized to perform this action!");
            }

            project = input.Map(project!);
            await repository.Project.EditAsync(c => c.Id.Equals(id), project);
            return new Payload("Project updated successfully", true);
        }

        /// <summary>
        /// Deletes certification records
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> DeleteProjectAsync(Guid id, [Service] IRepositoryManager repository,
            [Service] UserManager<Professional> userManager)
        {
            var userId = repository.User.GetLoggedInUserId();
            var isValidUser = await userId.ValidateLoggedinUser(userManager);
            if (!isValidUser.IsSuccessful || isValidUser.User == null)
            {
                return new Payload(isValidUser.Message);
            }

            var certification = await repository.Project.FindAsync(c => c.Id.Equals(id));
            if (certification.IsNull())
            {
                return new Payload("Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(certification!.UserId)))
            {
                return new Payload("You're not authorized to perform this action!");
            }

            await repository.Project.DeleteAsync(c => c.Id.Equals(id));

            return new Payload("Project deleted successfully", true);
        }
    }
}
