using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Data.Implementations
{
    public class RepositoryManager(MongoDbSettings settings, IHttpContextAccessor contextAccessor,
        UserManager<Professional> userManager) : IRepositoryManager
    {
        private readonly Lazy<IEducationRepository> educationRepository = new(()
            => new EducationRepository(settings));
        private readonly Lazy<IUserRepository> userRepository = new(()
            => new UserRepository(contextAccessor, userManager));
        private readonly Lazy<IOneTimePassRepository> oneTimePassRepository = new(()
            => new OneTimePassRepository(settings));
        private readonly Lazy<ICertificationRepository> certificationRepository = new(()
            => new CertificationRepository(settings));
        private readonly Lazy<ProfessionalSummaryRepository> professionalSummaryRepository = new(()
            => new ProfessionalSummaryRepository(settings));
        private readonly Lazy<IProjectRepository> projectRepository = new(()
            => new ProjectRepository(settings));
        private readonly Lazy<ISkillRepository> skillRepository = new(() 
            => new SkillRepository(settings));
        private readonly Lazy<IWorkExperienceRepository> workExperienceRepository = new(()
            => new WorkExperienceRepository(settings));
        private readonly Lazy<IFaqsRepository> faqsRepository = new(()
            => new FaqsRepository(settings));

        public IEducationRepository Education => educationRepository.Value;
        public IUserRepository User => userRepository.Value;
        public IOneTimePassRepository OneTimePass => oneTimePassRepository.Value;
        public ICertificationRepository Certification => certificationRepository.Value;
        public ISkillRepository Skill => skillRepository.Value;
        public IWorkExperienceRepository WorkExperience => workExperienceRepository.Value;
        public IProjectRepository Project => projectRepository.Value;
        public IProfessionalSummaryRepository Summary => professionalSummaryRepository.Value;
        public IFaqsRepository Faqs => faqsRepository.Value;
    }
}
