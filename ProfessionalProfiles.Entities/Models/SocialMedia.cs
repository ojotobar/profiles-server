using CSharpTypes.Extensions.Enumeration;
using ProfessionalProfiles.Entities.Enums;

namespace ProfessionalProfiles.Entities.Models
{
    public class SocialMedia
    {
        public string Name => Type.GetDescription();
        public string Link { get; set; } = string.Empty;
        public ESocialMedia Type { get; set; }
        public string IconName { get; set; } = string.Empty;
    }
}
