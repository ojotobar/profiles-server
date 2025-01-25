namespace ProfessionalProfiles.Data.Interface
{
    public interface IRepositoryManager
    {
        IEducationRepository Education { get; }
        IUserRepository User { get; }
        IOneTimePassRepository OneTimePass { get; }
    }
}
