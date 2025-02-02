using ProfessionalProfiles.Entities.Enums;
using ProfessionalProfiles.Graph.General;

namespace ProfessionalProfiles.Graph.Educations
{
    public class EducationInput
    {
        public string SchoolName { get; set; } = string.Empty;
        public EEducationLevel Level { get; set; }
        public string Course { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public EntityLocationInput Location { get; set; } = new();
    }
}
