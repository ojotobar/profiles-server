using ProfessionalProfiles.Graph.Dto;

namespace ProfessionalProfiles.GraphQL.Profile
{
    public record ProfilePayload(ProfessionalDto? Profile, string Message, bool Successful = false);
}