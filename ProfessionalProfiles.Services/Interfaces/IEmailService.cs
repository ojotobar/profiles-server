using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendAccountConfirmationEmail(Professional user, string origin);
        Task<bool> SendAccountRecoveryEmail(Professional user, string origin);
    }
}
