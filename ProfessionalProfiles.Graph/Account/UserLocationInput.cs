using ProfessionalProfiles.Graph.General;

namespace ProfessionalProfiles.Graph.Account
{
    public class UserLocationInput : EntityLocationInput
    {
        public string Line1 { get; set; } = string.Empty;
        public string? Line2 { get; set; }
        public string PostalCode { get; set; } = string.Empty;
    }
}
