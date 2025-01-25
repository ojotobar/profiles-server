using Mongo.Common.MongoDB;
using Mongo.Common.Settings;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;
using System.Linq.Expressions;

namespace ProfessionalProfiles.Data.Implementations
{
    public class EducationRepository : Repository<Education>, IEducationRepository
    {
        public EducationRepository(MongoDbSettings settings) 
            : base(settings) {}

        public async Task AddAsync(Education education) =>
            await CreateAsync(education);

        public async Task AddManyAsync(List<Education> educations) =>
            await CreateManyAsync(educations);

        public async Task EditAsync(Expression<Func<Education, bool>> expression, Education entity) =>
            await UpdateAsync(expression, entity);

        public async Task<List<Education>> FindManyAsync(Expression<Func<Education, bool>> expression) =>
            await GetManyAsync(expression);

        public async Task<Education?> FindOneAsync(Expression<Func<Education, bool>> expression) =>
            await GetAsync(expression);

        public IQueryable<Education> FindAsQueryable(Expression<Func<Education, bool>> expression) =>
            GetAsQueryable(expression);
    }
}
