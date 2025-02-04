using DRY.MailJetClient.Library;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using ProfessionalProfiles.Services.Interfaces;

namespace ProfessionalProfiles.Services.Implementations
{
    public class ServiceManager(IMailjetClientService mailjet, IRepositoryManager repository,
        UserManager<Professional> userManager, SignInManager<Professional> signInManager,
        IConfiguration configuration, ILogger<FirebaseService> firebaseLogger) : IServiceManager
    {
        private readonly Lazy<IEmailService> _mailjetService = new(() 
            => new EmailService(mailjet, repository, userManager));
        private readonly Lazy<IUserService> _userService = new(()
            => new UserService(userManager, signInManager, configuration));
        private readonly Lazy<IFirebaseService> _firebaseService = new(()
           => new FirebaseService(configuration, firebaseLogger));

        public IEmailService Email => _mailjetService.Value;
        public IUserService User => _userService.Value;
        public IFirebaseService Firebase => _firebaseService.Value;
    }
}
