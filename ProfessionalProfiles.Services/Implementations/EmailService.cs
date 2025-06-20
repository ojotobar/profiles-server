using DRY.MailJetClient.Library;
using Microsoft.AspNetCore.Identity;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Services.Interfaces;
using ProfessionalProfiles.Shared.Extensions;

namespace ProfessionalProfiles.Services.Implementations
{
    public class EmailService : IEmailService
    {
        private readonly IMailjetClientService mailJet;
        private readonly IRepositoryManager repository;
        private readonly UserManager<Professional> userManager;
        private readonly BackgroundJobsWorker jobsWorker;

        public EmailService(IMailjetClientService mailJet, IRepositoryManager repository,
            UserManager<Professional> userManager, BackgroundJobsWorker jobsWorker)
        {
            this.mailJet = mailJet;
            this.repository = repository;
            this.userManager = userManager;
            this.jobsWorker = jobsWorker;
        }

        public async Task<bool> SendAccountConfirmationEmail(Professional user, string origin)
        {
            var code = StringTypeExtensions.GenerateOtp();
            var pass = new OneTimePass { Otp = code, UserId = user.Id, ExpiresOn = DateTime.UtcNow.AddHours(1), PassType = EOtpType.Verification };
            await repository.OneTimePass.AddAsync(pass);
            var rootTemplate = GetRootTempltate(origin);
            var message = GetAccountVerifucationTemplate(user.FirstName, code, rootTemplate);
            return await mailJet.SendAsync(user.Email!, message, "Verify Your Account Email");
        }

        public async Task<bool> SendStatusChangeEmail(string origin, string email, string name, EStatus status)
        {
            var statusSpecificMessage = string.Empty;
            var subject = string.Empty;
            var appName = "Pro-files";

            switch (status)
            {
                case EStatus.Active:
                    subject = "Account Reactivation";
                    statusSpecificMessage = "Your account has been successfully reactivated.\r\nYou can now log in and continue using your account.";
                    break;
                case EStatus.Inactive:
                    subject = "Account Deactivation";
                    statusSpecificMessage = $"We’re writing to let you know that your account on {appName} has been deactivated as of {DateTime.UtcNow:D}.\r\nYou will no longer be able to access your account or associated services while it remains deactivated.\r\n⚠️ Please note: You have 180 days from the date of deactivation to request reactivation. After this period, your account and all associated data may be permanently deleted and will not be recoverable.\r\nIf you believe this was done in error or wish to reactivate your account, please contact us.";
                    break;
                case EStatus.Suspended:
                    subject = "Account Suspension";
                    statusSpecificMessage = "Your account has been suspended due to breach of one or more of our policies.\r\nIf you believe this was a mistake or you need further assistance, please contact support.";
                    break;
                case EStatus.Deleted:
                    subject = "Account Deletion";
                    statusSpecificMessage = $"We’re confirming that your account on {appName} has been permanently deleted as of {DateTime.UtcNow:D}.\r\nAll associated data have been removed from our system in accordance with our data retention policy.\r\nThis action is irreversible.\r\nIf you did not request this deletion or believe it was done in error, please contact us immediately";
                    break;
                default:
                    break;
            }
            var message = GetStatusChangeTemplate(name, statusSpecificMessage, origin, status);
            return await mailJet.SendAsync(email, message, subject);
        }

        public async Task<bool> SendRoleChangeEmail(string origin, string email, string name, string role)
        {
            var specificMessage = $"We’re reaching out to let you know that your account role on Pro-files has been updated.\r\n🔄 New Role: {role}  \r\nEffective Date: {DateTime.UtcNow:D}\r\nThis change may affect the permissions and features available to you within the platform. If you have any questions about your new role or believe this update was made in error, feel free to contact our support team.";            
            var message = GetRoleChangeTemplate(name, specificMessage, origin);
            return await mailJet.SendAsync(email, message, $"Role Update to {role}");
        }

        public async Task<bool> SendAccountRecoveryEmail(Professional user, string origin)
        {
            var code = StringTypeExtensions.GenerateOtp();
            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var pass = new OneTimePass 
            { 
                Otp = code, 
                UserId = user.Id, 
                ExpiresOn = DateTime.UtcNow.AddHours(1), 
                PassType = EOtpType.PasswordReset,
                Token = token
            };

            await repository.OneTimePass.AddAsync(pass);
            var rootTemplate = GetRootTempltate(origin);
            var message = GetAccountRecoveryTemplate(user.FirstName, code, rootTemplate);
            return await mailJet.SendAsync(user.Email!, message, "Reset Your Password");
        }

        public async Task SendDeployNotificationEmailAsync(List<Professional>? users, string tag)
        {
            if(users == null || users.Count <= 0)
            {
                return;
            }

            var dictionary = users.ToDictionary(user => user.Email, user => user.FirstName);
            var template = GetNewDeploymentTempltate(tag);
            if (!string.IsNullOrWhiteSpace(template) && dictionary != null)
            {
                await jobsWorker.SendNewDeploymentNofication(dictionary, template, $"Portfolio ({tag}) Update Available");
            }
        }

        #region Get Template Section
        private string GetRootTempltate(string origin)
        {
            string body = string.Empty;
            var folderName = System.IO.Path.Combine("wwwroot", "Templates", "RootTemplate.html");
            var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), folderName);

            if (File.Exists(filepath))
                body = File.ReadAllText(filepath);
            else
                return body;

            var msgBody = body.Replace("[[company_name]]", "Pro-files").
                Replace("[[base_url]]", origin).
                Replace("[[curr_year]]", DateTime.UtcNow.Year.ToString());

            return msgBody;
        }

        private string GetNewDeploymentTempltate(string tag)
        {
            string body = string.Empty;
            var folderName = System.IO.Path.Combine("wwwroot", "Templates", "NewDeployment.html");
            var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), folderName);

            if (File.Exists(filepath))
                body = File.ReadAllText(filepath);
            else
                return body;

            var msgBody = body.
                Replace("{{tag}}", tag);

            return msgBody;
        }

        private string GetAccountVerifucationTemplate(string name, string otp, string rootTemplate)
        {
            string body = string.Empty;
            var folderName = System.IO.Path.Combine("wwwroot", "Templates", "WelcomeEmail.html");
            var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), folderName);

            if (File.Exists(filepath))
                body = File.ReadAllText(filepath);
            else
                return body;

            var msgBody = body.Replace("[[company_name]]", "Pro-files")
                .Replace("[[recipient_name]]", name)
                .Replace("[[expiration_time]]", "1 hour")
                .Replace("[[activation_code]]", otp);

            return rootTemplate.Replace("[[specific_message]]", msgBody);
        }

        private string GetStatusChangeTemplate(string name, string statusSpecificMessage, string origin, EStatus status)
        {
            var rootTemplate = GetRootTempltate(origin);
            string body = string.Empty;
            var folderName = System.IO.Path.Combine("wwwroot", "Templates", "StatusChange.html");
            var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), folderName);

            if (File.Exists(filepath))
                body = File.ReadAllText(filepath);
            else
                return body;

            var msgBody = body.Replace("[[user_first_name]]", name)
                .Replace("[[status_specific_message]]", statusSpecificMessage)
                .Replace("[[base_url]]", origin)
                .Replace("[[company_name]]", "Pro-files")
                .Replace("[[email]]", "suport@pro-files.com");

            return rootTemplate.Replace("[[specific_message]]", msgBody);
        }

        private string GetRoleChangeTemplate(string name, string specificMessage, string origin)
        {
            var rootTemplate = GetRootTempltate(origin);
            string body = string.Empty;
            var folderName = System.IO.Path.Combine("wwwroot", "Templates", "StatusChange.html");
            var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), folderName);

            if (File.Exists(filepath))
                body = File.ReadAllText(filepath);
            else
                return body;

            var msgBody = body.Replace("[[user_first_name]]", name)
                .Replace("[[status_specific_message]]", specificMessage)
                .Replace("[[base_url]]", origin)
                .Replace("[[company_name]]", "Pro-files")
                .Replace("[[email]]", "suport@pro-files.com");

            return rootTemplate.Replace("[[specific_message]]", msgBody);
        }


        private string GetAccountRecoveryTemplate(string name, string otp, string rootTemplate)
        {
            string body = string.Empty;
            var folderName = System.IO.Path.Combine("wwwroot", "Templates", "AccountRecovery.html");
            var filepath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), folderName);

            if (File.Exists(filepath))
                body = File.ReadAllText(filepath);
            else
                return body;

            var msgBody = body.Replace("[[company_name]]", "Pro-files")
                .Replace("[[recipient_name]]", name)
                .Replace("[[expiration_time]]", "1 hour")
                .Replace("[[activation_code]]", otp);

            return rootTemplate.Replace("[[specific_message]]", msgBody);
        }
        #endregion
    }
}
