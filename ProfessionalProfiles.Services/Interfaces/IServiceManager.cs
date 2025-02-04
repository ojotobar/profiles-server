namespace ProfessionalProfiles.Services.Interfaces
{
    public interface IServiceManager
    {
        IEmailService Email { get; }
        IUserService User { get; }
        IFirebaseService Firebase { get; }
    }
}
