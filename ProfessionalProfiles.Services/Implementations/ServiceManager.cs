using DRY.MailJetClient.Library;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Services.Interfaces;

namespace ProfessionalProfiles.Services.Implementations
{
    public class ServiceManager(IMailjetClientService mailjet, IRepositoryManager repository,
        UserManager<Professional> userManager, SignInManager<Professional> signInManager,
        IConfiguration configuration) : IServiceManager
    {
        private readonly Lazy<IEmailService> _mailjetService = new(() 
            => new EmailService(mailjet, repository, userManager));
        private readonly Lazy<IUserService> _userService = new(()
            => new UserService(userManager, signInManager, configuration));

        public IEmailService Email => _mailjetService.Value;
        public IUserService User => _userService.Value;
    }
}
