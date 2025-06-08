using FluentValidation;
using ProfessionalProfiles.Graph.Common;

namespace ProfessionalProfiles.Graph.Validations
{
    public class SocialMediaInputValidator : AbstractValidator<SocialMediaInput>
    {
        public SocialMediaInputValidator()
        {
            RuleFor(x => x.Type)
                .IsInEnum().WithMessage("Invalid social media type");
            RuleFor(x => x.Link)
                .NotEmpty().WithMessage("Social media link is required.");
        }
    }
}
