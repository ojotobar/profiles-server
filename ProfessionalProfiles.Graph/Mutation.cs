using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Account;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.Educations;
using ProfessionalProfiles.Graph.General;
using ProfessionalProfiles.Graph.Profile;
using ProfessionalProfiles.Graph.Validations.Account;
using ProfessionalProfiles.Graph.Validations.Education;
using ProfessionalProfiles.Services.Interfaces;
using System.Net;

namespace ProfessionalProfiles.Graph
{
    public class Mutation
    {
        private const long MAXIMAGESIZE = 524228;//512kb
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
        public async Task<UserCommonPayload> RegisterUserAsync(RegisterUserInput input,
            [Service] UserManager<Professional> userManager, [GlobalState] string? origin,
            [Service] IRepositoryManager repository, [Service] IServiceManager service)
        {
            if (!input.MatchPassword)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "Password and Confirm Password fields must match", HttpStatusCode.BadRequest));
            }

            var user = await userManager.FindByEmailAsync(input.EmailAddress);
            if (user != null)
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
            if (input.IsNull() || input.Email.IsNullOrEmpty() || input.Password.IsNullOrEmpty())
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
                if (input.CodeType == EOtpType.Verification)
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
            [Service] UserManager<Professional> userManager, IRepositoryManager repository)
        {
            var validator = new UserLocationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input! Please try again.";
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, message, HttpStatusCode.BadRequest));
            }

            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await ValidateLoggedinUser(userId, userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize("", userValidationResult.Message, userValidationResult.StatusCode));
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

            userValidationResult.User.Location = location;
            await userManager.UpdateAsync(userValidationResult.User);
            return new UserCommonPayload(UserGenericPayload.Initialize(userValidationResult.User.Email!, "Location successfully added", HttpStatusCode.OK, true));
        }

        /// <summary>
        /// Upload user profile photo
        /// </summary>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <param name="file"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<UploadResult> UploadProfilePhotoAsync([Service]UserManager<Professional> userManager, 
            [Service]IRepositoryManager repository, [Service] IServiceManager service, IFile file)
        {
            var imageValidationResult = ValidateImageFile(file);
            if (!imageValidationResult.Payload.IsSuccessful)
            {
                return new UploadResult(Guid.Empty, "", imageValidationResult.Payload.Message);
            }

            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await ValidateLoggedinUser(userId, userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new UploadResult(Guid.Empty, "", userValidationResult.Message);
            }

            var user = userValidationResult.User;
            var fileName = GetFileName(user, file, ECloudFolder.ProfilePics);
            await using Stream stream = file.OpenReadStream();
            var uploadResult = await service.Firebase.UploadFileAsync(stream, ECloudFolder.ProfilePics, fileName, CancellationToken.None);
            if (uploadResult.Success)
            {
                user.ProfilePicture = uploadResult.Link;
                await userManager.UpdateAsync(user);
                return new UploadResult(user.Id, uploadResult.Link, "Profile picture successfully uploaded", true);
            }

            return new UploadResult(user.Id, "", "Upload to server failed. Please try again.");
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
        public async Task<UploadResult> UploadResumeAsync([Service] UserManager<Professional> userManager,
            [Service] IRepositoryManager repository, [Service] IServiceManager service, IFile file)
        {
            var imageValidationResult = ValidateDocFiles(file);
            if (!imageValidationResult.Payload.IsSuccessful)
            {
                return new UploadResult(Guid.Empty, "", imageValidationResult.Payload.Message);
            }

            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await ValidateLoggedinUser(userId, userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new UploadResult(Guid.Empty, string.Empty, userValidationResult.Message);
            }

            var user = userValidationResult.User;
            var fileName = GetFileName(user, file, ECloudFolder.Resume);
            await using Stream stream = file.OpenReadStream();
            var uploadResult = await service.Firebase.UploadFileAsync(stream, ECloudFolder.Resume, fileName, CancellationToken.None);
            if (uploadResult.Success)
            {
                user.ResumeLink = uploadResult.Link;
                await userManager.UpdateAsync(user);
                return new UploadResult(user.Id, uploadResult.Link, "File successfully uploaded", uploadResult.Success);
            }

            return new UploadResult(user.Id, string.Empty, "Upload to server failed. Please try again.");
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
        public async Task<UserCommonPayload> AddEducationAsync(EducationInput input,
            IRepositoryManager repository, [Service] UserManager<Professional> userManager)
        {
            var validator = new AddEducationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, message, HttpStatusCode.BadRequest));
            }

            var loggedInUserId = repository.User.GetLoggedInUserId();
            if (loggedInUserId.IsNullOrEmpty())
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "", HttpStatusCode.Unauthorized));
            }

            var user = await userManager.FindByIdAsync(loggedInUserId);
            if (user == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "User not found", HttpStatusCode.NotFound));
            }

            var education = EducationDto.CreateMap(loggedInUserId, input);
            await repository.Education.AddAsync(education);
            return new UserCommonPayload(UserGenericPayload.Initialize(user!.Email, "Education record added successfully", HttpStatusCode.OK, true));
        }

        /// <summary>
        /// Update education records
        /// </summary>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<UserCommonPayload> UpdateEducationAsync(Guid id, EducationInput input,
            IRepositoryManager repository, [Service] UserManager<Professional> userManager)
        {
            var validator = new AddEducationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, message, HttpStatusCode.BadRequest));
            }

            var existingRecord = await repository.Education.FindOneAsync(e => e.Id.Equals(id));
            if (existingRecord == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "Record not found", HttpStatusCode.NotFound));
            }

            existingRecord = EducationDto.CreateMap(existingRecord, input);
            await repository.Education.EditAsync(e => e.Id.Equals(existingRecord.Id), existingRecord);
            return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "Education record updated successfully", HttpStatusCode.OK, true));
        }

        /// <summary>
        /// Deprecates education records
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<UserCommonPayload> DeleteEducationAsync(Guid id, [Service] IRepositoryManager repository)
        {
            var existingRecord = await repository.Education.FindOneAsync(e => !e.IsDeprecated && e.Id.Equals(id));
            if (existingRecord == null)
            {
                return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "Record not found", HttpStatusCode.NotFound));
            }

            existingRecord.IsDeprecated = true;
            existingRecord.UpdatedOn = DateTime.UtcNow;
            await repository.Education.EditAsync(e => e.Id.Equals(existingRecord.Id), existingRecord);
            return new UserCommonPayload(UserGenericPayload.Initialize(string.Empty, "Education record deleted successfully", HttpStatusCode.OK, true));
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
