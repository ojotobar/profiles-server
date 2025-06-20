using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Services.Interfaces
{
    public interface IEmailService
    {
        Task<bool> SendAccountConfirmationEmail(Professional user, string origin);
        Task<bool> SendAccountRecoveryEmail(Professional user, string origin);
        Task SendDeployNotificationEmailAsync(List<Professional>? users, string tag);
        Task<bool> SendRoleChangeEmail(string origin, string email, string name, string role);
        Task<bool> SendStatusChangeEmail(string origin, string email, string name, EStatus status);
    }
}
