using Mongo.Common;

namespace ProfessionalProfiles.Entities.Models
{
    public class AdminArea : IBaseEntity
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid CountryId { get; set; }
        public Country? Country { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
        public bool IsDeprecated { get; set; }
    }
}
