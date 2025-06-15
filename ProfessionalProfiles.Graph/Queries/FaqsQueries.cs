using HotChocolate.Authorization;
using ProfessionalProfiles.Data.Interface;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph.Queries
{
    [ExtendObjectType(typeof(Query))]
    public class FaqsQueries
    {
        /// <summary>
        /// Gets paginated FAQs
        /// </summary>
        /// <param name="repository"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        [UseOffsetPaging(IncludeTotalCount = true)]
        public IQueryable<Faqs> GetFaqs([Service] IRepositoryManager repository,
            string? search)
        {
            var faqs = repository.Faqs.FindAsQueryable(f => !f.IsDeprecated);
            if (!string.IsNullOrWhiteSpace(search))
            {
                faqs = faqs.Where(f => f.Title.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
                    f.Content.Contains(search, StringComparison.CurrentCultureIgnoreCase));
            }

            return faqs.OrderByDescending(f => f.CreatedOn);
        }

        /// <summary>
        /// Get FAQs record by Id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="repository"></param>
        /// <returns></returns>
        [Authorize(Roles = ["Admin"])]
        public async Task<Faqs?> GetFaqAsync(Guid id, [Service] IRepositoryManager repository)
        {
            return await repository
                .Faqs.FindOneAsync(f => f.Id.Equals(id));
        }
    }
}
