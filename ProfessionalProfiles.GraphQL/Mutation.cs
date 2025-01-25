using CSharpTypes.Extensions.Enumeration;
using CSharpTypes.Extensions.List;
using HotChocolate;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.GraphQL.Account;
using System.Net;
using ProfessionalProfiles.Services.Interfaces;
using ProfessionalProfiles.GraphQL.General;
using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.String;
using CSharpTypes.Extensions.Object;
using MongoDB.Driver;
using HotChocolate.Authorization;
using ProfessionalProfiles.GraphQL.Validations.Account;
using ProfessionalProfiles.GraphQL.Educations;
using ProfessionalProfiles.GraphQL.Validations.Education;

namespace ProfessionalProfiles.GraphQL
{
    public class Mutation
    {
        #region Account Section
        /// <summary>
        /// Registers a new user
        /// </summary>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        public async Task<UserCommonPayload> RegisterUserAsync(RegisterUserInput input, 
            [Service] UserManager<Professional> userManager, [GlobalState] string? origin, 
            [Service] IRepositoryManager repository, [Service] IServiceManager service)
        {
            if (!input.MatchPassword)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "Password and Confirm Password fields must match", HttpStatusCode.BadRequest));
            }

            var user = await userManager.FindByEmailAsync(input.EmailAddress);
            if(user != null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "A user already exists with this email", HttpStatusCode.Conflict));
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
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Registration failed. {result.Errors.FirstOrDefault()?.Description}", HttpStatusCode.BadRequest));
            }

            var loggedInUserRoles = await repository.User.GetUserRoles();
            var role = loggedInUserRoles.IsNotNullOrEmpty() && loggedInUserRoles.Contains(ERoles.Admin) ?
                input.Role ?? ERoles.Professional : ERoles.Professional;

            var roleResult = await userManager.AddToRoleAsync(user, role.GetDescription());
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Registration failed. {roleResult.Errors.FirstOrDefault()?.Description}", HttpStatusCode.BadRequest));
            }

            var mailSent = await service.Email.SendAccountConfirmationEmail(user, origin!);
            if (!mailSent)
            {
                await userManager.DeleteAsync(user);
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Registration failed. Please try again.", HttpStatusCode.BadRequest));
            }
            return new UserCommonPayload(UserGenericPayload.Initialize(user.Email, "User registration successful. Please verify your account.", HttpStatusCode.OK, true));
        }

        /// <summary>
        /// Verifies User accounts
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public async Task<UserCommonPayload> VerifyAccountAsync(VerifyAccountInput input,
            [Service] UserManager<Professional> userManager, [Service] IRepositoryManager repository)
        {
            if (input.Email.IsNullOrEmpty() || input.OTP.IsNullOrEmpty())
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Invalid email or verification code. Please try again.", HttpStatusCode.BadRequest));
            }

            var user = await userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"No user found with the email: {input.Email}", HttpStatusCode.BadRequest));
            }

            var code = await repository.OneTimePass
                .FindOneAsync(c => 
                    c.UserId.Equals(user.Id) && c.Otp.Equals(input.OTP) && 
                    c.PassType.Equals(EOtpType.Verification) && c.ExpiresOn > DateTime.UtcNow && 
                    !c.Used && !c.IsDeprecated);

            if (code.IsNull())
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Invalid or expired verification code. Please generate a new one to continue.", HttpStatusCode.BadRequest));
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

            return new UserCommonPayload(UserGenericPayload.Initialize(user?.Email!, "Account successfully verified. Please proceed to log in.", HttpStatusCode.OK, true));
        }

        /// <summary>
        /// Logs in users
        /// </summary>
        /// <param name="input"></param>
        /// <param name="service"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        public async Task<LoginPayload> LoginUserAsync(LoginInput input,
            [Service] IServiceManager service, [Service] UserManager<Professional> userManager)
        {
            if(input.IsNull() || input.Email.IsNullOrEmpty() || input.Password.IsNullOrEmpty())
            {
                return new LoginPayload(LoginAccess.Initialize(input.Email, message: "Invalid request."));
            }

            var user = await userManager.FindByEmailAsync(input.Email);
            if (user.IsNull())
            {
                return new LoginPayload(LoginAccess.Initialize(input.Email, message: $"No user found with the email: {input.Email}"));
            }

            var validate = await service.User.Validate(input.Email, input.Password);
            if (!validate.Successful)
            {
                return new LoginPayload(LoginAccess.Initialize(input.Email, message: validate.Message!));
            }

            var tokenDto = await service.User.CreateAccessToken(validate, user!);

            return new LoginPayload(LoginAccess.Initialize(tokenDto.UserName!, tokenDto.AccessToken, validate.Message!, tokenDto.Successful));
        }

        /// <summary>
        /// Resend password reset or account activation code
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="origin"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task<UserCommonPayload> ResendCodeAsync(ResendCodeInput input,
            [Service] UserManager<Professional> userManager, [GlobalState] string? origin,
            [Service] IServiceManager service)
        {
            var user = await userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "No user found with this email", HttpStatusCode.NotFound));
            }

            if (user.EmailConfirmed && input.CodeType == EOtpType.Verification)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "Account already verified. Please login", HttpStatusCode.Found));
            }

            var mailSent = input.CodeType == EOtpType.Verification ? 
                await service.Email.SendAccountConfirmationEmail(user, origin!) : 
                await service.Email.SendAccountRecoveryEmail(user, origin!);
            if (!mailSent)
            {
                if(input.CodeType == EOtpType.Verification)
                {
                    await userManager.DeleteAsync(user);
                }
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Could not resend {input.CodeType.GetDescription()} code.", HttpStatusCode.BadRequest));
            }
            return new UserCommonPayload(UserGenericPayload.Initialize(user.Email!, $"{input.CodeType.GetDescription()} successfully sent. Please check your email", HttpStatusCode.OK, true));
        }

        /// <summary>
        /// Reset password
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="origin"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task<UserCommonPayload> ResetPasswordAsync(ResetPassInput input,
            [Service] UserManager<Professional> userManager, [GlobalState] string? origin,
            [Service] IServiceManager service)
        {
            var user = await userManager.FindByEmailAsync(input.Email);

            if (user == null || !user.EmailConfirmed)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Could not find a user with the email: {input.Email}, or account not confirmed yet.", HttpStatusCode.NotFound));
            }

            var mailSent = await service.Email.SendAccountRecoveryEmail(user, origin!);

            if (!mailSent)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Could not send password code.", HttpStatusCode.BadRequest));
            }
            return new UserCommonPayload(UserGenericPayload.Initialize(user.Email!, $"Password reset code successfully sent. Please check your email", HttpStatusCode.OK, true));
        }

        /// <summary>
        /// Change forgotten password
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public async Task<UserCommonPayload> ChangeForgottenPasswordAsync(ForgotPasswordInput input,
            [Service] UserManager<Professional> userManager, [Service] IRepositoryManager repository)
        {
            var user = await userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Could not find the user with email: {input.Email}.", HttpStatusCode.NotFound));
            }

            var code = await repository.OneTimePass
                .FindOneAsync(c =>
                    c.Otp.Equals(input.Code) &&
                    c.PassType.Equals(EOtpType.PasswordReset) && c.ExpiresOn > DateTime.UtcNow &&
                    !c.Used && !c.IsDeprecated);

            if (code == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Invalid or expired password reset code. Please generate a new one to continue.", HttpStatusCode.BadRequest));
            }

            var result = await userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(code.Token), input.NewPassword);
            if (!result.Succeeded)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"{result.Errors.FirstOrDefault()?.Description}", HttpStatusCode.BadRequest));
            }

            return new UserCommonPayload(UserGenericPayload.Initialize(user.Email!, $"Password reset successfully. Please proceed to login.", HttpStatusCode.OK, true));
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<UserCommonPayload> ChangePassword(ChangePasswordInput input,
            [Service] UserManager<Professional> userManager, [Service] IRepositoryManager repository)
        {
            if (input.CurrentPassword.IsNullOrEmpty() || input.NewPassword.IsNullOrEmpty())
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Invalid request", HttpStatusCode.BadRequest));
            }
            var loggedInUserId = repository.User.GetLoggedInUserId();
            var user = await userManager.FindByIdAsync(loggedInUserId);
            if (user == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"Access denied", HttpStatusCode.Unauthorized));
            }

            var result = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
            if (!result.Succeeded)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, $"{result.Errors.FirstOrDefault()?.Description}", HttpStatusCode.BadRequest));
            }

            return new UserCommonPayload(UserGenericPayload.Initialize(user.Email!, $"Password changed successfully.", HttpStatusCode.OK, true));
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
        public async Task<UserCommonPayload> AddUserLocationAsync(UserLocationInput input,
            [Service]UserManager<Professional> userManager, IRepositoryManager repository)
        {
            var validator = new UserLocationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input! Please try again.";
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, message, HttpStatusCode.BadRequest));
            }

            var userId = repository.User.GetLoggedInUserId();
            if(userId.IsNullOrEmpty())
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "Access denied", HttpStatusCode.Unauthorized));
            }

            var user = await userManager.FindByIdAsync(userId);
            if(user == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "No user found", HttpStatusCode.NotFound));
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

            user.Location = location;
            await userManager.UpdateAsync(user);
            return new UserCommonPayload(UserGenericPayload.Initialize(user.Email!, "Location successfully added", HttpStatusCode.OK, true));
        }
        #endregion

        #region Education Region
        /// <summary>
        /// Add education records
        /// </summary>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<UserCommonPayload> AddEducationAsync(EducationInput input,
            IRepositoryManager repository, [Service] UserManager<Professional> userManager)
        {
            var loggedInUserId = repository.User.GetLoggedInUserId();
            if (loggedInUserId.IsNullOrEmpty())
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "", HttpStatusCode.Unauthorized));
            }

            var user = await userManager.FindByIdAsync(loggedInUserId);
            if(user == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "User not found", HttpStatusCode.NotFound));
            }

            var validator = new AddEducationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, message, HttpStatusCode.BadRequest));
            }

            var education = new Education
            {
                InstitutionName = input.SchoolName,
                Level = input.Level,
                Major = input.Course,
                StartDate = input.StartDate,
                EndDate = input.EndDate,
                UserId = loggedInUserId,
                Location = new EntityLocation
                {
                    City = input.Location.City,
                    Country = input.Location.Country,
                    State = input.Location.State,
                    Longitude = input.Location.Longitude,
                    Latitude = input.Location.Latitude
                }
            };

            await repository.Education.AddAsync(education);
            return new UserCommonPayload(UserGenericPayload.Initialize(user!.Email, "Education record added successfully", HttpStatusCode.OK, true));
        }
        #endregion
    }
}