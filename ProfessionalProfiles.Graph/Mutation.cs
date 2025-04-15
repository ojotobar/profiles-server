using CSharpTypes.Extensions.Enumeration;
using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Account;
using ProfessionalProfiles.Graph.CareerSummaries;
using ProfessionalProfiles.Graph.Certfications;
using ProfessionalProfiles.Graph.Common;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.Educations;
using ProfessionalProfiles.Graph.Experiences;
using ProfessionalProfiles.Graph.General;
using ProfessionalProfiles.Graph.Projects;
using ProfessionalProfiles.Graph.Skills;
using ProfessionalProfiles.Graph.Validations;
using ProfessionalProfiles.Services.Implementations;
using ProfessionalProfiles.Services.Interfaces;
using System.Net;

namespace ProfessionalProfiles.Graph
{
    public class Mutation
    {
        private const long MAXIMAGESIZE = 1572684;//1.5mb
        private const long MAXSKILLCOUNT = 20;
        private readonly List<string> ALLOWEDIMAGEFORMATS = [".png", ".jpg", ".jpeg"];
        private readonly List<string> ALLOWEDDOCFORMATS = [".pdf", ".docx", ".doc"];

        #region Account Section
        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        public async Task<Payload> RegisterUserAsync(RegisterUserInput input,
            [Service] UserManager<Professional> userManager, [GlobalState] string? origin,
            [Service] IRepositoryManager repository, [Service] IServiceManager service)
        {
            if (!input.MatchPassword)
            {
                return new Payload("Password and Confirm Password fields must match");
            }

            var user = await userManager.FindByEmailAsync(input.EmailAddress);
            if (user != null)
            {
                return new Payload("A user already exists with this email");
            }

            user = new Professional
            {
                Email = input.EmailAddress,
                FirstName = input.FirstName,
                LastName = input.LastName,
                PhoneNumber = input.PhoneNumber,
                UserName = input.EmailAddress,
                Gender = input.Gender,
                OtherName = input.MiddleName
            };

            var result = await userManager.CreateAsync(user, input.Password);
            if (!result.Succeeded)
            {
                return new Payload($"Registration failed. {result.Errors.FirstOrDefault()?.Description}");
            }

            var loggedInUserRoles = await repository.User.GetUserRoles();
            var role = loggedInUserRoles.IsNotNullOrEmpty() && loggedInUserRoles.Contains(ERoles.Admin) ?
                input.Role ?? ERoles.Professional : ERoles.Professional;

            var roleResult = await userManager.AddToRoleAsync(user, role.GetDescription());
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                return new Payload($"Registration failed. {roleResult.Errors.FirstOrDefault()?.Description}");
            }

            var mailSent = await service.Email.SendAccountConfirmationEmail(user, origin!);
            if (!mailSent)
            {
                await userManager.DeleteAsync(user);
                return new Payload($"Registration failed. Please try again.");
            }
            return new Payload("User registration successful. Please verify your account.", true);
        }

        /// <summary>
        /// Verifies User accounts
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public async Task<Payload> VerifyAccountAsync(VerifyAccountInput input,
            [Service] UserManager<Professional> userManager, [Service] IRepositoryManager repository,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog)
        {
            if (input.Email.IsNullOrEmpty() || input.OTP.IsNullOrEmpty())
            {
                return new Payload($"Invalid email or verification code. Please try again.");
            }

            var user = await userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return new Payload($"No user found with the email: {input.Email}");
            }

            var code = await repository.OneTimePass
                .FindOneAsync(c =>
                    c.UserId.Equals(user.Id) && c.Otp.Equals(input.OTP) &&
                    c.PassType.Equals(EOtpType.Verification) && c.ExpiresOn > DateTime.UtcNow &&
                    !c.Used && !c.IsDeprecated);

            if (code.IsNull())
            {
                return new Payload($"Invalid or expired verification code. Please generate a new one to continue.");
            }

            //Update user
            user.UpdatedOn = DateTime.UtcNow;
            user.EmailConfirmed = true;
            user.Status = EStatus.Active;
            await userManager.UpdateAsync(user);
            // Update code
            code!.UpdatedOn = DateTime.UtcNow;
            code.IsDeprecated = true;
            code.Used = true;
            await repository.OneTimePass.EditAsync(c => c.Id.Equals(code.Id), code);

            if (auditLog != null)
            {
                auditLog.ActionId = EAction.VerifiedAccount;
                auditLog.Action = auditLog.ActionId.GetDescription();
                auditLog.UserId = user.Id.ToString();
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload("Account successfully verified. Please proceed to log in.", true);
        }

        /// <summary>
        /// Logs in users
        /// </summary>
        /// <param name="input"></param>
        /// <param name="service"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        public async Task<LoginResult> LoginUserAsync(LoginInput input, [Service] IServiceManager service, 
            [Service] UserManager<Professional> userManager, [Service] BackgroundJobsWorker auditLogger, 
            [GlobalState] string? origin, [GlobalState] AuditLog? auditLog)
        {
            if (input.IsNull() || input.Email.IsNullOrEmpty() || input.Password.IsNullOrEmpty())
            {
                return new LoginResult("", "", $"Invalid request");
            }

            var user = await userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return new LoginResult("", input.Email, $"No user found with the email: {input.Email}");
            }

            var validate = await service.User.Validate(input.Email, input.Password);
            if (!validate.Successful)
            {
                if (validate.EmailNotConfirmed)
                {
                    await service.Email.SendAccountConfirmationEmail(user, origin ?? "");
                }

                return new LoginResult("", input.Email, validate.Message!, EmailNotConfirmed: validate.EmailNotConfirmed);
            }

            var tokenDto = await service.User.CreateAccessToken(validate, user!);
            if(auditLog != null)
            {
                auditLog.ActionId = EAction.LoggedIn;
                auditLog.Action = auditLog.ActionId.GetDescription();
                auditLog.UserId = user.Id.ToString();
                await auditLogger.LogAuditAsync(auditLog);
            }
            
            return new LoginResult(tokenDto.AccessToken, tokenDto.UserName, validate.Message!, true);
        }

        /// <summary>
        /// Resend password reset or account activation code
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="origin"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task<Payload> ResendCodeAsync(ResendCodeInput input,
            [Service] UserManager<Professional> userManager, [GlobalState] string? origin,
            [Service] IServiceManager service)
        {
            var user = await userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return new Payload("No user found with this email");
            }

            if (user.EmailConfirmed && input.CodeType == EOtpType.Verification)
            {
                return new Payload("Account already verified. Please login");
            }

            var mailSent = input.CodeType == EOtpType.Verification ?
                await service.Email.SendAccountConfirmationEmail(user, origin!) :
                await service.Email.SendAccountRecoveryEmail(user, origin!);
            if (!mailSent)
            {
                if (input.CodeType == EOtpType.Verification)
                {
                    await userManager.DeleteAsync(user);
                }
                return new Payload($"Could not resend {input.CodeType.GetDescription()} code.");
            }
            return new Payload($"{input.CodeType.GetDescription()} successfully sent. Please check your email", true);
        }

        /// <summary>
        /// Reset password
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="origin"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task<Payload> ResetPasswordAsync(ResetPassInput input, [Service] UserManager<Professional> userManager, 
            [GlobalState] string? origin, [Service] IServiceManager service, [GlobalState] AuditLog? auditLog,
            [Service] BackgroundJobsWorker auditLogger)
        {
            var user = await userManager.FindByEmailAsync(input.Email);

            if (user == null || !user.EmailConfirmed)
            {
                return new Payload($"Could not find a user with the email: {input.Email}, or account not confirmed yet.");
            }

            var mailSent = await service.Email.SendAccountRecoveryEmail(user, origin!);

            if (!mailSent)
            {
                return new Payload($"Could not send password code.");
            }

            if (auditLog != null)
            {
                auditLog.ActionId = EAction.PasswordReset;
                auditLog.Action = auditLog.ActionId.GetDescription();
                auditLog.UserId = user.Id.ToString();
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload($"Password reset code successfully sent. Please check your email", true);
        }

        /// <summary>
        /// Change forgotten password
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public async Task<Payload> ChangeForgottenPasswordAsync(ForgotPasswordInput input,
            [Service] UserManager<Professional> userManager, [Service] IRepositoryManager repository,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog? auditLog)
        {
            var user = await userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return new Payload($"Could not find the user with email: {input.Email}.");
            }

            var code = await repository.OneTimePass
                .FindOneAsync(c =>
                    c.Otp.Equals(input.Code) &&
                    c.PassType.Equals(EOtpType.PasswordReset) && c.ExpiresOn > DateTime.UtcNow &&
                    !c.Used && !c.IsDeprecated);

            if (code == null)
            {
                return new Payload($"Invalid or expired password reset code. Please generate a new one to continue.");
            }

            // Update code
            code!.UpdatedOn = DateTime.UtcNow;
            code.IsDeprecated = true;
            code.Used = true;
            await repository.OneTimePass.EditAsync(c => c.Id.Equals(code.Id), code);

            var result = await userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(code.Token), input.NewPassword);
            if (!result.Succeeded)
            {
                return new Payload($"{result.Errors.FirstOrDefault()?.Description}");
            }

            if (auditLog != null)
            {
                auditLog.ActionId = EAction.ForgottenPasswordChange;
                auditLog.Action = auditLog.ActionId.GetDescription();
                auditLog.UserId = user.Id.ToString();
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload($"Password reset successfully. Please proceed to login.", true);
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> ChangePassword(ChangePasswordInput input,
            [Service] UserManager<Professional> userManager, [Service] IRepositoryManager repository,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog? auditLog)
        {
            if (input.CurrentPassword.IsNullOrEmpty() || input.NewPassword.IsNullOrEmpty())
            {
                return new Payload("Invalid request");
            }

            if (!input.NewPassword.Equals(input.ConfirmNewPassword))
            {
                return new Payload($"New Password and Comfirm New Password fields must match. Please try again");
            }

            var loggedInUserId = repository.User.GetLoggedInUserId();
            var user = await userManager.FindByIdAsync(loggedInUserId);
            if (user == null)
            {
                return new Payload("Access denied");
            }

            var result = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
            if (!result.Succeeded)
            {
                return new Payload($"{result.Errors.FirstOrDefault()?.Description}");
            }

            if (auditLog != null)
            {
                auditLog.ActionId = EAction.PasswordChange;
                auditLog.Action = auditLog.ActionId.GetDescription();
                auditLog.UserId = user.Id.ToString();
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload("Password changed successfully. Please login with the new password", true);
        }
        #endregion

        #region Profile Section
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
            var userValidationResult = await ValidateLoggedinUser(userId, userManager);
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
            if(userValidationResult.User.Location != null)
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
        /// Upload user profile photo
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UploadProfilePhotoAsync([Service]UserManager<Professional> userManager, 
            [Service]IRepositoryManager repository, [Service] IServiceManager service, IFile file,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog)
        {
            var imageValidationResult = ValidateImageFile(file);
            if (!imageValidationResult.Payload.IsSuccessful)
            {
                return new Payload(imageValidationResult.Payload.Message);
            }

            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await ValidateLoggedinUser(userId, userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new Payload(userValidationResult.Message);
            }

            var user = userValidationResult.User;
            var fileName = GetFileName(user, file, ECloudFolder.ProfilePics);
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
            var imageValidationResult = ValidateDocFiles(file);
            if (!imageValidationResult.Payload.IsSuccessful)
            {
                return new Payload(imageValidationResult.Payload.Message);
            }

            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await ValidateLoggedinUser(userId, userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new Payload(userValidationResult.Message);
            }

            var user = userValidationResult.User;
            var fileName = GetFileName(user, file, ECloudFolder.Resume);
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
            var userValidationResult = await ValidateLoggedinUser(userId, userManager);
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
            if(user == null)
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
                auditLog.ActionId = EAction.StatusChange;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), previousStatus.GetDescription(), user.Status.GetDescription());
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
            if(previousStatus == input.NewStatus)
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
            if(auditLog != null)
            {
                auditLog.ActionId = EAction.StatusChange;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), previousStatus.GetDescription(), user.Status.GetDescription());
                auditLog.UserId = performer;
                auditLog.PerformedOn = user.Email;
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
            if(role == null)
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
            if(roleResult == null || !roleResult.Succeeded)
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
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), previousRole, input.Role.GetDescription());
                auditLog.UserId = performer;
                auditLog.PerformedOn = user.Email;
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
            if(result == null || !result.Succeeded)
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
                auditLog.PerformedOn = user.Email;
                await auditLogger.LogAuditAsync(auditLog);
            }

            await auditLogger.SendStatusChangeEmailAsync(origin!, user.Email!, user.FirstName, EStatus.Deleted);

            return new Payload($"Account successfully deleted for {userId}.", true);
        }
        #endregion

        #region Education Section
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
        public async Task<Payload> DeleteEducationAsync(Guid id, [Service] IRepositoryManager repository)
        {
            var existingRecord = await repository.Education.FindOneAsync(e => !e.IsDeprecated && e.Id.Equals(id));
            if (existingRecord == null)
            {
                return new Payload("Record not found");
            }

            existingRecord.IsDeprecated = true;
            existingRecord.UpdatedOn = DateTime.UtcNow;
            await repository.Education.EditAsync(e => e.Id.Equals(existingRecord.Id), existingRecord);
            return new Payload("Education record deleted successfully", true);
        }
        #endregion

        #region Certification Section
        /// <summary>
        /// Add User Certification
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> AddCertificationsAsync(List<CertificationInput> inputs,
            IRepositoryManager repository)
        {
            foreach (var input in inputs)
            {
                var validationResult = new CertificationInputValidator().Validate(input);
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

            var certifications = inputs.Initialize(userId);
            await repository.Certification.AddRangeAsync(certifications);
            return new Payload("Certification added successfully", true);
        }

        /// <summary>
        /// Updated certification
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> UpdateCertificationAsync(Guid id, CertificationInput input,
            IRepositoryManager repository)
        {
            var validationResult = new CertificationInputValidator().Validate(input);
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

            var certification = await repository.Certification.FindAsync(c => c.Id.Equals(id));
            if (certification.IsNull())
            {
                return new Payload("Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(certification!.UserId)))
            {
                return new Payload("You're not authorized to perform this action!");
            }

            certification = input.Map(certification!);
            await repository.Certification.EditAsync(c => c.Id.Equals(id), certification);
            return new Payload("Certification updated successfully", true);
        }

        /// <summary>
        /// Deletes certification records
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> DeleteCertificationAsync(Guid id, IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
            }

            var certification = await repository.Certification.FindAsync(c => c.Id.Equals(id));
            if (certification.IsNull())
            {
                return new Payload("Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(certification!.UserId)))
            {
                return new Payload("You're not authorized to perform this action!");
            }

            await repository.Certification.DeleteAsync(c => c.Id.Equals(id));
            return new Payload("Certification deleted successfully", true);
        }
        #endregion

        #region Career Summary Section
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
        public async Task<Payload> DeleteCareerSummaryAsync(Guid id, IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
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
            return new Payload("Professional Summary record deleted successfully", true);
        }
        #endregion

        #region Project Section
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
        public async Task<Payload> DeleteProjectAsync(Guid id, IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
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
        #endregion

        #region Experience Section
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
        public async Task<Payload> DeleteExperienceAsync(Guid id, IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new Payload("Permission denied!!!");
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
            return new Payload("Experience deleted successfully", true);
        }
        #endregion

        #region User Skill Section
        /// <summary>
        /// Adds a list of user skills
        /// </summary>
        /// <param name="inputs"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<Payload> AddSkillsAsync(List<SkillInput> inputs, IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInOrApiKeyUserId("");
            if (userId.IsEmpty())
            {
                return new Payload("Access denied!!!");
            }

            if (!inputs.IsNotNullOrEmpty())
            {
                return new Payload("You must enter one or more skills");
            }

            var count = await repository.Skill.CountAllAsync(s => s.UserId.Equals(userId));
            if(count >= MAXSKILLCOUNT || MAXSKILLCOUNT < (count + inputs.Count))
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
            if(skillToUpdate == null)
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
        public async Task<Payload> DeleteSkillAsync(Guid id, IRepositoryManager repository)
        {
            var skillToDelete = await repository.Skill.FindAsync(s => s.Id.Equals(id) && !s.IsDeprecated);
            if (skillToDelete == null)
            {
                return new Payload("No record found for the provided id");
            }

            await repository.Skill.DeleteAsync(s => s.Id.Equals(skillToDelete.Id));
            return new Payload("Skill deleted successful", true);
        }
        #endregion

        #region Validations
        private UserCommonPayload ValidateImageFile(IFile file)
        {
            if (file.IsNull() || file.Length <= 0)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", "Invalid file", HttpStatusCode.BadRequest));
            }

            if (file.Length > MAXIMAGESIZE)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", $"File size exceeds limit of {MAXIMAGESIZE / 1024}kb", HttpStatusCode.BadRequest));
            }

            if (!ALLOWEDIMAGEFORMATS.Any(f => file.Name.EndsWith(f)))
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", $"Invalid image format. Allowed formats: {string.Join(", ", ALLOWEDIMAGEFORMATS)}", HttpStatusCode.BadRequest));
            }

            return new UserCommonPayload(UserGenericPayload.Initialize("", "", HttpStatusCode.OK, true));
        }

        private UserCommonPayload ValidateDocFiles(IFile file)
        {
            if (file.IsNull() || file.Length <= 0)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", "Invalid file", HttpStatusCode.BadRequest));
            }

            if (file.Length > MAXIMAGESIZE)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", $"File size exceeds limit of {MAXIMAGESIZE / 1024}kb", HttpStatusCode.BadRequest));
            }

            if (!ALLOWEDDOCFORMATS.Any(f => file.Name.EndsWith(f)))
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", $"Invalid document format. Allowed formats: {string.Join(", ", ALLOWEDIMAGEFORMATS)}", HttpStatusCode.BadRequest));
            }

            return new UserCommonPayload(UserGenericPayload.Initialize("", "", HttpStatusCode.OK, true));
        }

        private async Task<UserValidationPayload> ValidateLoggedinUser(string userId, UserManager<Professional> userManager)
        {
            if (userId.IsNullOrEmpty())
            {
                return new UserValidationPayload(null, "Access denied", HttpStatusCode.Unauthorized);
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return new UserValidationPayload(null, "No user found", HttpStatusCode.NotFound);
            }

            return new UserValidationPayload(user, "", HttpStatusCode.OK, true);
        }

        private string GetFileName(Professional user, IFile file, ECloudFolder folder)
        {
            string ext = System.IO.Path.GetExtension(file.Name);
            var fileName = file.Name.Replace(" ", "_");
            if (ext.IsNotNullOrEmpty())
            {
                var split = user.Id.ToString().Split("-");
                var idSnippet = split.Length > 0 ? $"_{split[0]}" : "";
                fileName = folder == ECloudFolder.Resume ? 
                    $"{user.LastName}_{user.FirstName}{idSnippet}_CV{ext}" :
                    $"{user.LastName}_{user.FirstName}{idSnippet}{ext}"; ;
            }

            return fileName;
        }
        #endregion
    }
}
