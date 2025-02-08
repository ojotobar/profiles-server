namespace ProfessionalProfiles.Data.Interface
{
    public interface IRepositoryManager
    {
        IEducationRepository Education { get; }
        IUserRepository User { get; }
        IOneTimePassRepository OneTimePass { get; }
        ICertificationRepository Certification { get; }
        IProfessionalSkillRepository ProfessionalSkill { get; }
        ISkillRepository Skill { get; }
        IWorkExperienceRepository WorkExperience { get; }
        IProjectRepository Project { get; }
    }
}
