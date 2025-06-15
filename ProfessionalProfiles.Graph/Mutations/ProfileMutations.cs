using CSharpTypes.Extensions.Enumeration;
using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.String;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Account;
using ProfessionalProfiles.Graph.Common;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.Validations;
using ProfessionalProfiles.Services.Implementations;
using ProfessionalProfiles.Services.Interfaces;
using ProfessionalProfiles.Graph.Extensions;

namespace ProfessionalProfiles.Graph.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class ProfileMutations
    {
        /// <summary>
        /// Adds user current location
        /// </summary>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> AddOrUpdateUserLocationAsync(UserLocationInput input,
            [Service] UserManager<Professional> userManager, IRepositoryManager repository,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog)
        {
            var validator = new UserLocationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input! Please try again.";
                return new Payload(message);
            }

            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await userId.ValidateLoggedinUser(userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new Payload(userValidationResult.Message);
            }

            var location = new ProfessionalLocation
            {
                City = input.City,
                Country = input.Country,
                Line1 = input.Line1,
                Line2 = input.Line2,
                PostalCode = input.PostalCode,
                State = input.State,
                Latitude = input.Latitude,
                Longitude = input.Longitude
            };

            var action = "added";
            if (userValidationResult.User.Location != null)
            {
                action = "updated";
            }
            userValidationResult.User.Location = location;
            await userManager.UpdateAsync(userValidationResult.User);
            if (auditLog != null)
            {
                auditLog.ActionId = EAction.ProfileUpdate;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), $"{action.Capitalize()} Location");
                auditLog.UserId = userId;
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload($"Location successfully {action}", true);
        }

        /// <summary>
        /// Add or Update User's Social Media Handles
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> AddOrUpdateSocialMediaAsync(ICollection<SocialMediaInput> inputs,
            [Service] UserManager<Professional> userManager, IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await userId.ValidateLoggedinUser(userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new Payload(userValidationResult.Message);
            }

            if (!inputs.IsNotNullOrEmpty())
            {
                return new Payload("One or more social media must be added.");
            }

            foreach (var input in inputs)
            {
                var validator = new SocialMediaInputValidator().Validate(input);
                if (!validator.IsValid)
                {
                    var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input! Please try again.";
                    return new Payload(message);
                }
            }

            var user = userValidationResult.User;
            var data = inputs.Map();
            user.SocialMedia = data.ToHashSet();
            await userManager.UpdateAsync(user);
            return new Payload($"Social Media successfully updated.", true);
        }

        /// <summary>
        /// Upload user profile photo
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UploadProfilePhotoAsync([Service] UserManager<Professional> userManager,
            [Service] IRepositoryManager repository, [Service] IServiceManager service, IFile file,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog)
        {
            var imageValidationResult = file.ValidateImageFile();
            if (!imageValidationResult.Payload.IsSuccessful)
            {
                return new Payload(imageValidationResult.Payload.Message);
            }

            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await userId.ValidateLoggedinUser(userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new Payload(userValidationResult.Message);
            }

            var user = userValidationResult.User;
            var fileName = user.GetFileName(file, ECloudFolder.ProfilePics);
            await using Stream stream = file.OpenReadStream();
            var uploadResult = await service.Firebase.UploadFileAsync(stream, ECloudFolder.ProfilePics, fileName, CancellationToken.None);
            if (uploadResult.Success)
            {
                user.ProfilePicture = uploadResult.Link;
                await userManager.UpdateAsync(user);
                if (auditLog != null)
                {
                    auditLog.ActionId = EAction.ProfileUpdate;
                    auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), $"Uploaded Profile Photo");
                    auditLog.UserId = userId;
                    await auditLogger.LogAuditAsync(auditLog);
                }

                return new Payload("Profile picture successfully uploaded", true);
            }

            return new Payload("Upload to server failed. Please try again.");
        }

        /// <summary>
        /// Upload User CV
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <param name="service"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UploadResumeAsync([Service] UserManager<Professional> userManager,
            [Service] IRepositoryManager repository, [Service] IServiceManager service, IFile file,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog)
        {
            var imageValidationResult = file.ValidateDocFiles();
            if (!imageValidationResult.Payload.IsSuccessful)
            {
                return new Payload(imageValidationResult.Payload.Message);
            }

            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await userId.ValidateLoggedinUser(userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new Payload(userValidationResult.Message);
            }

            var user = userValidationResult.User;
            var fileName = user.GetFileName(file, ECloudFolder.Resume);
            await using Stream stream = file.OpenReadStream();
            var uploadResult = await service.Firebase.UploadFileAsync(stream, ECloudFolder.Resume, fileName, CancellationToken.None);
            if (uploadResult.Success)
            {
                user.ResumeLink = uploadResult.Link;
                await userManager.UpdateAsync(user);
                if (auditLog != null)
                {
                    auditLog.ActionId = EAction.ProfileUpdate;
                    auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), $"Uploaded CV");
                    auditLog.UserId = userId;
                    await auditLogger.LogAuditAsync(auditLog);
                }

                return new Payload("File successfully uploaded", uploadResult.Success);
            }

            return new Payload("Upload to server failed. Please try again.");
        }

        /// <summary>
        /// Updates user's profile details
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UpdateProfileDetailsAsync(ProfileDetailsInput input,
            [Service] UserManager<Professional> userManager, [Service] IRepositoryManager repository,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog)
        {
            var validator = new ProfileDetailsInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new Payload(message);
            }

            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await userId.ValidateLoggedinUser(userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new Payload(userValidationResult.Message);
            }

            var user = userValidationResult.User;
            if (user == null)
            {
                return new Payload("Profile not found");
            }

            user.FirstName = input.FirstName;
            user.LastName = input.LastName;
            user.OtherName = input.OtherName;
            user.PhoneNumber = input.Phone;
            user.Gender = input.Gender;

            await userManager.UpdateAsync(user);
            if (auditLog != null)
            {
                auditLog.ActionId = EAction.ProfileUpdate;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), $"Profile Details");
                auditLog.UserId = userId;
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload("Profile details successfully updated", true);
        }

        /// <summary>
        /// Endpoint to clean up firebase storage
        /// </summary>
        /// <param name="input"></param>
        /// <param name="jobScheduler"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> CleanUpFirebase(FirebaseCleanupInput input,
            [Service] BackgroundJobsWorker jobScheduler)
        {
            await jobScheduler.CleanUpFirebaseStorage(input.DeleteAll);
            return new Payload("Clean Up Job scheduled...", true);
        }

        /// <summary>
        /// Remove user assets like photo and CV
        /// </summary>
        /// <param name="type"></param>
        /// <param name="service"></param>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> DeleteUserAssetAsync(ECloudFolder type,
            [Service] IServiceManager service, [Service] IRepositoryManager repository,
            [Service] UserManager<Professional> userManager)
        {
            var userId = repository.User.GetLoggedInUserId();
            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new Payload("User found");
            }

            var fileToDelete = user.GetFileName(type);
            if (string.IsNullOrEmpty(fileToDelete))
            {
                return new Payload("Could not get the file name to delete. Please try again later");
            }

            await service.Firebase.RemoveFileAsync(type, fileToDelete);
            if (type == ECloudFolder.ProfilePics)
            {
                user.ProfilePicture = string.Empty;
            }
            else
            {
                user.ResumeLink = string.Empty;
            }

            await userManager.UpdateAsync(user);
            return new Payload("File successfully deleted", true);
        }

        /// <summary>
        /// Deactivate account
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="auditLogger"></param>
        /// <param name="auditLog"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> DeactivateAccountAsync([Service] UserManager<Professional> userManager,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog,
            [Service] IRepositoryManager repository, [GlobalState] string? origin)
        {
            var loggedInUserId = repository.User.GetLoggedInUserId();
            if (loggedInUserId.IsNullOrEmpty())
            {
                return new Payload("Access denied! Invalid user credentials.");
            }

            var user = await userManager.FindByIdAsync(loggedInUserId);
            if (user == null)
            {
                return new Payload("No user found with the logged in user's credentials.");
            }

            var previousStatus = user.Status;

            user.Status = EStatus.Inactive;
            user.DeactivatedOn = DateTime.UtcNow;
            await userManager.UpdateAsync(user);

            var timeUntilDeletion = user.DeactivatedOn.AddDays(180);
            if (auditLog != null)
            {
                auditLog.ActionId = EAction.Deactivated;
                auditLog.Action = auditLog.ActionId.GetDescription();
                auditLog.UserId = repository.User.GetLoggedInUserId();
                await auditLogger.LogAuditAsync(auditLog);
            }

            await auditLogger.SendStatusChangeEmailAsync(origin ?? "", user.Email!, user.FirstName, user.Status);
            return new Payload($"You have successfully deactivated your account. Submit a reactivation request before {timeUntilDeletion} to continue using your account", true);
        }

        /// <summary>
        /// Service to change users' statuses
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="auditLogger"></param>
        /// <param name="auditLog"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> ChangeStatusAsync(ChangeStatusInput input, [Service] UserManager<Professional> userManager,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog? auditLog, [Service] IRepositoryManager repository,
            [GlobalState] string? origin)
        {
            var user = await userManager.FindByEmailAsync(input.UserEmail);
            if (user == null)
            {
                return new Payload($"No user found with the Email: {input.UserEmail}.");
            }

            var performer = repository.User.GetLoggedInUserId();
            if (string.IsNullOrEmpty(performer) || performer.Equals(user.Id.ToString()))
            {
                return new Payload($"You're not allowed to perform this operation on yourself");
            }

            var previousStatus = user.Status;
            if (previousStatus == input.NewStatus)
            {
                return new Payload($"User already in status {input.NewStatus.GetDescription()}");
            }

            switch (input.NewStatus)
            {
                case EStatus.Inactive:
                    user.Status = EStatus.Inactive;
                    user.DeactivatedOn = DateTime.UtcNow;
                    break;
                case EStatus.Active:
                    if (user.Status == EStatus.Inactive)
                    {
                        user.DeactivatedOn = DateTime.MaxValue;
                    }
                    else
                    {
                        user.IsDeprecated = false;
                    }
                    user.Status = EStatus.Active;
                    break;
                case EStatus.Suspended:
                    user.Status = EStatus.Suspended;
                    user.IsDeprecated = true;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid status {input.NewStatus} provided.");
            }

            await userManager.UpdateAsync(user);
            if (auditLog != null)
            {
                auditLog.ActionId = EAction.StatusChange;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), user.Email);
                auditLog.UserId = performer;
                await auditLogger.LogAuditAsync(auditLog);
            }

            await auditLogger.SendStatusChangeEmailAsync(origin ?? "", user.Email!, user.FirstName, user.Status);
            return new Payload($"Status successfully changed to {input.NewStatus} for {input.UserEmail}.", true);
        }

        /// <summary>
        /// Adds user to a defined role
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="auditLogger"></param>
        /// <param name="auditLog"></param>
        /// <param name="repository"></param>
        /// <param name="roleManager"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> ChangeRoleAsync(ChangeRoleInput input, [Service] UserManager<Professional> userManager,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog? auditLog, [Service] IRepositoryManager repository,
            [Service] RoleManager<AppRole> roleManager, [GlobalState] string? origin)
        {
            var role = await roleManager.FindByNameAsync(input.Role.GetDescription());
            if (role == null)
            {
                return new Payload($"No role found with the name: {input.Role.GetDescription()}.");
            }

            var user = await userManager.FindByEmailAsync(input.UserEmail);
            if (user == null)
            {
                return new Payload($"No user found with the Email: {input.UserEmail}.");
            }

            var performer = repository.User.GetLoggedInUserId();
            if (string.IsNullOrEmpty(performer) || performer.Equals(user.Id.ToString()))
            {
                return new Payload($"You're not allowed to perform this operation on yourself");
            }

            var previousRole = (await userManager.GetRolesAsync(user))?.FirstOrDefault();

            var roleResult = await userManager.AddToRoleAsync(user, role.Name ?? input.Role.GetDescription());
            if (roleResult == null || !roleResult.Succeeded)
            {
                return new Payload(roleResult?.Errors.FirstOrDefault()?.Description ?? "An error occurred. Could not update the user role");
            }

            if (!string.IsNullOrEmpty(previousRole))
            {
                await userManager.RemoveFromRoleAsync(user, previousRole);
            }

            if (auditLog != null)
            {
                auditLog.ActionId = EAction.RoleUpdate;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), user.Email);
                auditLog.UserId = performer;
                await auditLogger.LogAuditAsync(auditLog);
            }

            await auditLogger.SendRoleUpdateEmailAsync(origin!, user.Email!, user.FirstName, role.Name ?? input.Role.GetDescription());

            return new Payload($"Status successfully changed to {input.Role.GetDescription()} for {input.UserEmail}.", true);
        }

        /// <summary>
        /// Permanently deletes users' accounts and associated data
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userManager"></param>
        /// <param name="auditLogger"></param>
        /// <param name="auditLog"></param>
        /// <param name="repository"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> DeleteAccountAsync(Guid userId, [Service] UserManager<Professional> userManager,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog? auditLog,
            [Service] IRepositoryManager repository, [GlobalState] string? origin)
        {
            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user == null)
            {
                return new Payload($"No user found with the Id: {userId}.");
            }

            var performer = repository.User.GetLoggedInUserId();
            if (string.IsNullOrEmpty(performer) || performer.Equals(user.Id.ToString()))
            {
                return new Payload($"You're not allowed to perform this operation on yourself");
            }

            var result = await userManager.DeleteAsync(user);
            if (result == null || !result.Succeeded)
            {
                return new Payload(result?.Errors?.FirstOrDefault()?.Description ?? "Account deletion failed. Please try again");
            }

            await repository.Certification.DeleteRangeAsync(c => c.UserId == userId, CancellationToken.None);
            await repository.Education.DeleteRangeAsync(e => e.UserId == userId, CancellationToken.None);
            await repository.WorkExperience.DeleteRangeAsync(xp => xp.UserId.Equals(userId), CancellationToken.None);
            await repository.Skill.DeleteRangeAsync(s => s.UserId.Equals(userId), CancellationToken.None);
            await repository.Project.DeleteRangeAsync(p => p.UserId.Equals(userId), CancellationToken.None);
            await repository.Summary.DeleteAsync(s => s.UserId.Equals(userId));
            //TODO: Delete cv and photo from fire store

            if (auditLog != null)
            {
                auditLog.ActionId = EAction.Deleted;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), user.Email!);
                auditLog.UserId = performer;
                await auditLogger.LogAuditAsync(auditLog);
            }

            await auditLogger.SendStatusChangeEmailAsync(origin!, user.Email!, user.FirstName, EStatus.Deleted);

            return new Payload($"Account successfully deleted for {userId}.", true);
        }
    }
}
