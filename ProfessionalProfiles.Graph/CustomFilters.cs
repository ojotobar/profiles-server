using HotChocolate.Data.Filters;
using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Entities.Models;

namespace ProfessionalProfiles.Graph
{
    public static class CustomFilter
    {
        public static IQueryable<Professional> Filter(this IQueryable<Professional> data, UserFilterInput? query)
        {
            if (query != null)
            {
                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    data = data.Where(u => u.FirstName.Contains(query.Search, StringComparison.CurrentCultureIgnoreCase) ||
                        u.LastName.Contains(query.Search, StringComparison.CurrentCultureIgnoreCase) ||
                        (!string.IsNullOrEmpty(u.OtherName) && u.OtherName.Contains(query.Search, StringComparison.CurrentCultureIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(u.Email) && u.Email.Contains(query.Search, StringComparison.CurrentCultureIgnoreCase)));
                }

                if (query.Gender.HasValue)
                {
                    data = data.Where(u => u.Gender.Equals(query.Gender.Value));
                }

                if (query.Status.HasValue)
                {
                    data = data.Where(u => u.Status.Equals(query.Status.Value));
                }

                if (query.Premium.HasValue && query.Premium.Value)
                {
                    data = data.Where(u => u.IsPremium.Equals(query.Premium.Value));
                }

                if (query.Confirmed.HasValue && query.Confirmed.Value)
                {
                    data = data.Where(u => u.EmailConfirmed.Equals(query.Confirmed.Value));
                }
            }

            return data;
        }

        public static IQueryable<AuditLog> Filter(this IQueryable<AuditLog> data, AuditLogFilterInput? query)
        {
            if (query != null)
            {
                if (query.Action.HasValue)
                {
                    data = data.Where(a => a.ActionId.Equals(query.Action.Value));
                }

                if (!string.IsNullOrWhiteSpace(query.Search))
                {
                    data = data
                        .Where(a => !string.IsNullOrEmpty(a.PerformedBy) && 
                            a.PerformedBy.Contains(query.Search, StringComparison.CurrentCultureIgnoreCase));
                }
            }

            return data;
        }
    }

    public class AuditFilterType : FilterInputType<AuditLog>
    {
        protected override void Configure(IFilterInputTypeDescriptor<AuditLog> descriptor)
        {
            descriptor.Field(a => a.ActionId).Name("action");
        }
    }

    public class UserFilterInput : SearchModel
    {
        public EGender? Gender { get; set; }
        public EStatus? Status { get; set; }
        public bool? Premium { get; set; }
        public bool? Confirmed { get; set; }
    }

    public class AuditLogFilterInput : SearchModel
    {
        public EAction? Action { get; set; }
    }

    public class SearchModel
    {
        public string? Search { get; set; }
    }
}
