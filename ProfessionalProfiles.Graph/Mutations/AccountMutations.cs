using CSharpTypes.Extensions.Enumeration;
using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Account;
using ProfessionalProfiles.Graph.Common;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.General;
using ProfessionalProfiles.Services.Implementations;
using ProfessionalProfiles.Services.Interfaces;

namespace ProfessionalProfiles.Graph.Mutations
{
    [ExtendObjectType(typeof(Mutation))]
    public class AccountMutations
    {
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
            if (auditLog != null)
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

        /// <summary>
        /// Add a new role
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="roleManager"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> AddSystemRoleAsync(string roleName, [Service] RoleManager<AppRole> roleManager,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog,
            [Service] IRepositoryManager repository)
        {
            if (string.IsNullOrEmpty(roleName))
            {
                return new Payload("Role name is required.");
            }

            var exists = await roleManager.RoleExistsAsync(roleName);
            if (exists)
            {
                return new Payload($"The role, {roleName} already exists.");
            }

            var result = await roleManager.CreateAsync(new AppRole
            {
                Name = roleName,
                NormalizedName = roleName.ToUpper()
            });

            if (!result.Succeeded)
            {
                return new Payload(result.Errors.FirstOrDefault()?.Description ?? "An error occurred while adding the role");
            }

            if (auditLog != null)
            {
                auditLog.ActionId = EAction.RoleAdmin;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), "Added", $"{roleName}");
                auditLog.UserId = repository.User.GetLoggedInUserId();
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload($"The role, {roleName} successfully added. Be informed that there'll need to be other configurations done before users are added to this role", true);
        }

        /// <summary>
        /// Updates role
        /// </summary>
        /// <param name="input"></param>
        /// <param name="roleManager"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> UpdateSystemRoleAsync(AppRoleInput input,
            [Service] RoleManager<AppRole> roleManager, [Service] UserManager<Professional> userManager,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog,
            [Service] IRepositoryManager repository)
        {
            if (string.IsNullOrEmpty(input.Name))
            {
                return new Payload("Role name is required.");
            }

            var role = await roleManager.FindByIdAsync(input.Id.ToString());
            if (role == null)
            {
                return new Payload($"The role with this Id does not already exists.");
            }

            var hasUsers = userManager.Users.Any(u => u.Roles.Contains(role.Id));
            if (hasUsers)
            {
                return new Payload("Could not update role because we already have users added to the role");
            }

            var prevValue = role.Name;
            role.Name = input.Name;
            role.NormalizedName = input.Name.ToUpper();
            var result = await roleManager.UpdateAsync(role);

            if (!result.Succeeded)
            {
                return new Payload(result.Errors.FirstOrDefault()?.Description ?? "An error occurred while adding the role");
            }

            if (auditLog != null)
            {
                auditLog.ActionId = EAction.RoleAdmin;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), "Updated", $"{prevValue} to {input.Name}");
                auditLog.UserId = repository.User.GetLoggedInUserId();
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload($"The role, {input.Name} successfully Updated. Be informed that there'll need for other configurations to be done before users can are added to this role", true);
        }

        /// <summary>
        /// Deletes a role
        /// </summary>
        /// <param name="id"></param>
        /// <param name="roleManager"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Payload> DeleteSystemRoleAsync(Guid id,
            [Service] RoleManager<AppRole> roleManager, [Service] UserManager<Professional> userManager,
            [Service] BackgroundJobsWorker auditLogger, [GlobalState] AuditLog auditLog,
            [Service] IRepositoryManager repository)
        {
            var role = await roleManager.FindByIdAsync(id.ToString());
            if (role == null)
            {
                return new Payload($"The role with this Id does not already exists.");
            }

            var hasUsers = userManager.Users.Any(u => u.Roles.Contains(role.Id));
            if (hasUsers)
            {
                return new Payload("Could not delete role because we already have users added to the role");
            }

            var result = await roleManager.DeleteAsync(role);

            if (!result.Succeeded)
            {
                return new Payload(result.Errors.FirstOrDefault()?.Description ?? "An error occurred while adding the role");
            }

            if (auditLog != null)
            {
                auditLog.ActionId = EAction.RoleAdmin;
                auditLog.Action = string.Format(auditLog.ActionId.GetDescription(), "Deleted", $"{role.Name}");
                auditLog.UserId = repository.User.GetLoggedInUserId();
                await auditLogger.LogAuditAsync(auditLog);
            }

            return new Payload($"The role, {role.Name} successfully deleted.", true);
        }
    }
}