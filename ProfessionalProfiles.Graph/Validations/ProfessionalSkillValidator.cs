using FluentValidation;

namespace ProfessionalProfiles.Graph.Validations
{
    public class ProfessionalSkillValidator : AbstractValidator<string>
    {
        public ProfessionalSkillValidator()
        {
            RuleFor(s => s)
                .Must(ValidationExtensions.IsAphaWithSomeChars).WithMessage("Skill entries must not be null or empty space and can only contain spaces and letters A-Z.");
        }
    }
}
