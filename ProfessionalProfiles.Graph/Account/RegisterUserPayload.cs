using ProfessionalProfiles.Graph.General;

namespace ProfessionalProfiles.Graph.Account
{
    public record UserCommonPayload(UserGenericPayload Payload);
    public record LoginPayload(LoginAccess LoginAccess);
}
