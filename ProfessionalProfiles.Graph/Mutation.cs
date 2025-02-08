using CSharpTypes.Extensions.Guid;
using CSharpTypes.Extensions.List;
using CSharpTypes.Extensions.Object;
using CSharpTypes.Extensions.String;
using FluentValidation;
using FluentValidation.Results;
using HotChocolate.Authorization;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Graph.Account;
using ProfessionalProfiles.Graph.Certfications;
using ProfessionalProfiles.Graph.Dto;
using ProfessionalProfiles.Graph.Educations;
using ProfessionalProfiles.Graph.General;
using ProfessionalProfiles.Graph.Profile;
using ProfessionalProfiles.Graph.Validations.Account;
using ProfessionalProfiles.Graph.Validations.Certification;
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
        public async Task<AccountResult> RegisterUserAsync(RegisterUserInput input,
            [Service] UserManager<Professional> userManager, [GlobalState] string? origin,
            [Service] IRepositoryManager repository, [Service] IServiceManager service)
        {
            if (!input.MatchPassword)
            {
                return new AccountResult(string.Empty, "Password and Confirm Password fields must match");
            }

            var user = await userManager.FindByEmailAsync(input.EmailAddress);
            if (user != null)
            {
                return new AccountResult(string.Empty, "A user already exists with this email");
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
                return new AccountResult(string.Empty, $"Registration failed. {result.Errors.FirstOrDefault()?.Description}");
            }

            var loggedInUserRoles = await repository.User.GetUserRoles();
            var role = loggedInUserRoles.IsNotNullOrEmpty() && loggedInUserRoles.Contains(ERoles.Admin) ?
                input.Role ?? ERoles.Professional : ERoles.Professional;

            var roleResult = await userManager.AddToRoleAsync(user, role.GetDescription());
            if (!roleResult.Succeeded)
            {
                await userManager.DeleteAsync(user);
                return new AccountResult(string.Empty, $"Registration failed. {roleResult.Errors.FirstOrDefault()?.Description}");
            }

            var mailSent = await service.Email.SendAccountConfirmationEmail(user, origin!);
            if (!mailSent)
            {
                await userManager.DeleteAsync(user);
                return new AccountResult(string.Empty, $"Registration failed. Please try again.");
            }
            return new AccountResult(user.Email, "User registration successful. Please verify your account.", true);
        }

        /// <summary>
        /// Verifies User accounts
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public async Task<AccountResult> VerifyAccountAsync(VerifyAccountInput input,
            [Service] UserManager<Professional> userManager, [Service] IRepositoryManager repository)
        {
            if (input.Email.IsNullOrEmpty() || input.OTP.IsNullOrEmpty())
            {
                return new AccountResult(string.Empty, $"Invalid email or verification code. Please try again.");
            }

            var user = await userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return new AccountResult(string.Empty, $"No user found with the email: {input.Email}");
            }

            var code = await repository.OneTimePass
                .FindOneAsync(c =>
                    c.UserId.Equals(user.Id) && c.Otp.Equals(input.OTP) &&
                    c.PassType.Equals(EOtpType.Verification) && c.ExpiresOn > DateTime.UtcNow &&
                    !c.Used && !c.IsDeprecated);

            if (code.IsNull())
            {
                return new AccountResult(string.Empty, $"Invalid or expired verification code. Please generate a new one to continue.");
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

            return new AccountResult(user?.Email!, "Account successfully verified. Please proceed to log in.", true);
        }

        /// <summary>
        /// Logs in users
        /// </summary>
        /// <param name="input"></param>
        /// <param name="service"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        public async Task<LoginResult> LoginUserAsync(LoginInput input,
            [Service] IServiceManager service, [Service] UserManager<Professional> userManager)
        {
            if (input.IsNull() || input.Email.IsNullOrEmpty() || input.Password.IsNullOrEmpty())
            {
                return new LoginResult("", "", $"Invalid request");
            }

            var user = await userManager.FindByEmailAsync(input.Email);
            if (user.IsNull())
            {
                return new LoginResult("", input.Email, $"No user found with the email: {input.Email}");
            }

            var validate = await service.User.Validate(input.Email, input.Password);
            if (!validate.Successful)
            {
                return new LoginResult("", input.Email, validate.Message!);
            }

            var tokenDto = await service.User.CreateAccessToken(validate, user!);

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
        public async Task<AccountResult> ResendCodeAsync(ResendCodeInput input,
            [Service] UserManager<Professional> userManager, [GlobalState] string? origin,
            [Service] IServiceManager service)
        {
            var user = await userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return new AccountResult(string.Empty, "No user found with this email");
            }

            if (user.EmailConfirmed && input.CodeType == EOtpType.Verification)
            {
                return new AccountResult(string.Empty, "Account already verified. Please login");
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
                return new AccountResult(string.Empty, $"Could not resend {input.CodeType.GetDescription()} code.");
            }
            return new AccountResult(user.Email!, $"{input.CodeType.GetDescription()} successfully sent. Please check your email", true);
        }

        /// <summary>
        /// Reset password
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="origin"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        public async Task<AccountResult> ResetPasswordAsync(ResetPassInput input,
            [Service] UserManager<Professional> userManager, [GlobalState] string? origin,
            [Service] IServiceManager service)
        {
            var user = await userManager.FindByEmailAsync(input.Email);

            if (user == null || !user.EmailConfirmed)
            {
                return new AccountResult(string.Empty, $"Could not find a user with the email: {input.Email}, or account not confirmed yet.");
            }

            var mailSent = await service.Email.SendAccountRecoveryEmail(user, origin!);

            if (!mailSent)
            {
                return new AccountResult(string.Empty, $"Could not send password code.");
            }
            return new AccountResult(user.Email!, $"Password reset code successfully sent. Please check your email", true);
        }

        /// <summary>
        /// Change forgotten password
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        public async Task<AccountResult> ChangeForgottenPasswordAsync(ForgotPasswordInput input,
            [Service] UserManager<Professional> userManager, [Service] IRepositoryManager repository)
        {
            var user = await userManager.FindByEmailAsync(input.Email);
            if (user == null)
            {
                return new AccountResult(string.Empty, $"Could not find the user with email: {input.Email}.");
            }

            var code = await repository.OneTimePass
                .FindOneAsync(c =>
                    c.Otp.Equals(input.Code) &&
                    c.PassType.Equals(EOtpType.PasswordReset) && c.ExpiresOn > DateTime.UtcNow &&
                    !c.Used && !c.IsDeprecated);

            if (code == null)
            {
                return new AccountResult(string.Empty, $"Invalid or expired password reset code. Please generate a new one to continue.");
            }

            var result = await userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(code.Token), input.NewPassword);
            if (!result.Succeeded)
            {
                return new AccountResult(string.Empty, $"{result.Errors.FirstOrDefault()?.Description}");
            }

            return new AccountResult(user.Email!, $"Password reset successfully. Please proceed to login.", true);
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="input"></param>
        /// <param name="userManager"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<AccountResult> ChangePassword(ChangePasswordInput input,
            [Service] UserManager<Professional> userManager, [Service] IRepositoryManager repository)
        {
            if (input.CurrentPassword.IsNullOrEmpty() || input.NewPassword.IsNullOrEmpty())
            {
                return new AccountResult(string.Empty, $"Invalid request");
            }

            if (!input.NewPassword.Equals(input.ConfirmNewPassword))
            {
                return new AccountResult(string.Empty, $"New Password and Comfirm New Password fields must match. Please try again");
            }

            var loggedInUserId = repository.User.GetLoggedInUserId();
            var user = await userManager.FindByIdAsync(loggedInUserId);
            if (user == null)
            {
                return new AccountResult(string.Empty, $"Access denied");
            }

            var result = await userManager.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
            if (!result.Succeeded)
            {
                return new AccountResult(string.Empty, $"{result.Errors.FirstOrDefault()?.Description}");
            }

            return new AccountResult(user.Email!, $"Password changed successfully.", true);
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
        public async Task<AccountResult> AddUserLocationAsync(UserLocationInput input,
            [Service] UserManager<Professional> userManager, IRepositoryManager repository)
        {
            var validator = new UserLocationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input! Please try again.";
                return new AccountResult(string.Empty, message);
            }

            var userId = repository.User.GetLoggedInUserId();
            var userValidationResult = await ValidateLoggedinUser(userId, userManager);
            if (!userValidationResult.IsSuccessful || userValidationResult.User == null)
            {
                return new AccountResult("", userValidationResult.Message);
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
            return new AccountResult(userValidationResult.User.Email!, "Location successfully added", true);
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
        public async Task<EducationResult> AddEducationAsync(EducationInput input,
            IRepositoryManager repository, [Service] UserManager<Professional> userManager)
        {
            var validator = new AddEducationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new EducationResult(null, message);
            }

            var loggedInUserId = repository.User.GetLoggedInUserId();
            if (loggedInUserId.IsNullOrEmpty())
            {
                return new EducationResult(null, "Access denied");
            }

            var user = await userManager.FindByIdAsync(loggedInUserId);
            if (user == null)
            {
                return new EducationResult(null, "User not found");
            }

            var education = EducationDto.CreateMap(loggedInUserId, input);
            await repository.Education.AddAsync(education);
            return new EducationResult(education, "Education record added successfully", true);
        }

        /// <summary>
        /// Update education records
        /// </summary>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <param name="userManager"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<EducationResult> UpdateEducationAsync(Guid id, EducationInput input,
            IRepositoryManager repository, [Service] UserManager<Professional> userManager)
        {
            var validator = new AddEducationInputValidator().Validate(input);
            if (!validator.IsValid)
            {
                var message = validator.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new EducationResult(null, message);
            }

            var existingRecord = await repository.Education.FindOneAsync(e => e.Id.Equals(id) && !e.IsDeprecated);
            if (existingRecord == null)
            {
                return new EducationResult(null, "Record not found");
            }

            existingRecord = EducationDto.CreateMap(existingRecord, input);
            await repository.Education.EditAsync(e => e.Id.Equals(existingRecord.Id), existingRecord);
            return new EducationResult(existingRecord, "Education record updated successfully", true);
        }

        /// <summary>
        /// Deprecates education records
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<EducationResult> DeleteEducationAsync(Guid id, [Service] IRepositoryManager repository)
        {
            var existingRecord = await repository.Education.FindOneAsync(e => !e.IsDeprecated && e.Id.Equals(id));
            if (existingRecord == null)
            {
                return new EducationResult(null, "Record not found");
            }

            existingRecord.IsDeprecated = true;
            existingRecord.UpdatedOn = DateTime.UtcNow;
            await repository.Education.EditAsync(e => e.Id.Equals(existingRecord.Id), existingRecord);
            return new EducationResult(null, "Education record deleted successfully", true);
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
        public async Task<CertificationsPayload> AddCertificationsAsync(List<CertificationInput> inputs,
            IRepositoryManager repository)
        {
            foreach (var input in inputs)
            {
                var validationResult = new CertificationInputValidator().Validate(input);
                if (!validationResult.IsValid)
                {
                    var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                    return new CertificationsPayload([], message);
                }
            }

            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new CertificationsPayload([], "Permission denied!!!");
            }

            var certifications = inputs.Initialize(userId);
            await repository.Certification.AddRangeAsync(certifications);
            return new CertificationsPayload(certifications, "Certification added successfully", true);
        }

        /// <summary>
        /// Updated certification
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize]
        public async Task<CertificationPayload> UpdateCertificationAsync(Guid id, CertificationInput input,
            IRepositoryManager repository)
        {
            var validationResult = new CertificationInputValidator().Validate(input);
            if (!validationResult.IsValid)
            {
                var message = validationResult.Errors.FirstOrDefault()?.ErrorMessage ?? "Invalid input";
                return new CertificationPayload(null, message);
            }

            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new CertificationPayload(null, "Permission denied!!!");
            }

            var certification = await repository.Certification.FindAsync(c => c.Id.Equals(id));
            if (certification.IsNull())
            {
                return new CertificationPayload(null, "Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(certification!.UserId)))
            {
                return new CertificationPayload(null, "You're not authorized to perform this action!");
            }

            certification = input.Map(certification!);
            await repository.Certification.EditAsync(c => c.Id.Equals(id), certification);
            return new CertificationPayload(certification, "Certification updated successfully", true);
        }

        [Authorize]
        public async Task<CertificationPayload> DeleteCertificationAsync(Guid id, IRepositoryManager repository)
        {
            var userId = repository.User.GetLoggedInUserId().ToGuid();
            if (userId.IsEmpty())
            {
                return new CertificationPayload(null, "Permission denied!!!");
            }

            var certification = await repository.Certification.FindAsync(c => c.Id.Equals(id));
            if (certification.IsNull())
            {
                return new CertificationPayload(null, "Record not found");
            }

            var loggedInUserRole = await repository.User.GetUserRoles();
            if (!loggedInUserRole.IsNotNullOrEmpty() || (!loggedInUserRole.Contains(ERoles.Admin) && !userId.Equals(certification!.UserId)))
            {
                return new CertificationPayload(null, "You're not authorized to perform this action!");
            }

            await repository.Certification.DeleteAsync(c => c.Id.Equals(id));
            return new CertificationPayload(null, "Certification deleted successfully", true);
        }
        #endregion

        #region User Skill Section
        #endregion

        #region Career Summary Section
        #endregion

        #region Project Section
        #endregion

        #region Skill Section
        #endregion

        #region Experience Section
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
