namespace ProfessionalProfiles.GraphQL.General
{
    public class EntityLocationInput
    {
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? Longitude { get; set; } = string.Empty;
        public string? Latitude { get; set; } = string.Empty;
    }
}
