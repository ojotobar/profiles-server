namespace ProfessionalProfiles.Data.Interface
{
    public interface IRepositoryManager
    {
        IEducationRepository Education { get; }
        IUserRepository User { get; }
        IOneTimePassRepository OneTimePass { get; }
        ICertificationRepository Certification { get; }
        ISkillRepository Skill { get; }
        IWorkExperienceRepository WorkExperience { get; }
        IProjectRepository Project { get; }
        IProfessionalSummaryRepository Summary { get; }
        IFaqsRepository Faqs { get; }
    }
}
