using ProfessionalProfiles.GraphQL.General;

namespace ProfessionalProfiles.GraphQL.Account
{
    public record UserCommonPayload(UserGenericPayload Payload);
    public record LoginPayload(LoginAccess LoginAccess);
}
