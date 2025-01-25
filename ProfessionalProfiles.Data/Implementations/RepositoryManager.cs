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
        private readonly Lazy<IEducationRepository> educationRepository = new Lazy<IEducationRepository>(()
            => new EducationRepository(settings));
        private readonly Lazy<IUserRepository> userRepository = new Lazy<IUserRepository>(()
            => new UserRepository(contextAccessor, userManager));
        private readonly Lazy<IOneTimePassRepository> oneTimePassRepository = new Lazy<IOneTimePassRepository>(()
            => new OneTimePassRepository(settings));

        public IEducationRepository Education => educationRepository.Value;
        public IUserRepository User => userRepository.Value;
        public IOneTimePassRepository OneTimePass => oneTimePassRepository.Value;
    }
}
