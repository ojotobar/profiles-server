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

        public EmailService(IMailjetClientService mailJet, IRepositoryManager repository,
            UserManager<Professional> userManager)
        {
            this.mailJet = mailJet;
            this.repository = repository;
            this.userManager = userManager;
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

            var msgBody = body.Replace("[[company_name]]", "Profession Profiles").
                Replace("[[base_url]]", origin).
                Replace("[[curr_year]]", DateTime.UtcNow.Year.ToString());

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

            var msgBody = body.Replace("[[company_name]]", "Profession Profiles")
                .Replace("[[recipient_name]]", name)
                .Replace("[[expiration_time]]", "1 hour")
                .Replace("[[activation_code]]", otp);

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

            var msgBody = body.Replace("[[company_name]]", "Profession Profiles")
                .Replace("[[recipient_name]]", name)
                .Replace("[[expiration_time]]", "1 hour")
                .Replace("[[activation_code]]", otp);

            return rootTemplate.Replace("[[specific_message]]", msgBody);
        }
        #endregion
    }
}
