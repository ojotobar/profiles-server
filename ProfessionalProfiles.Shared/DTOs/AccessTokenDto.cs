namespace ProfessionalProfiles.Shared.DTOs
{
    public class AccessTokenDto
    {
        public string AccessToken { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public bool Successful { get; set; }
        public string? Message { get; set; }
    }
}
