using ProfessionalProfiles.Entities.Enums;
namespace ProfessionalProfiles.Graph.Common
{
    public record Payload(string Message, bool Success = false);
    public record ChangeStatusInput(string UserEmail, EStatus NewStatus);
    public record ChangeRoleInput(string UserEmail, ERoles Role);
    public record FaqsInput(string Title, string Content);
    public record SocialMediaInput(ESocialMedia Type, string Link, string? IconName);
}
