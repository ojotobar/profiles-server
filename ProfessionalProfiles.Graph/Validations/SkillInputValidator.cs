using FluentValidation;
using ProfessionalProfiles.Graph.Skills;

namespace ProfessionalProfiles.Graph.Validations
{
    public class SkillInputValidator : AbstractValidator<SkillInput>
    {
        public SkillInputValidator()
        {
            RuleFor(s => s.Name)
                .Must(ValidationExtensions.IsAphaWithSomeChars).WithMessage("Skill Name is required and can not contain some special characters.");
            RuleFor(s => s.Level)
                .IsInEnum().WithMessage("Please enter a valid skill level");
            RuleFor(s => s.Years)
                .Must(ValidationExtensions.BeAPositiveInteger)
                .WithMessage("Years of experience must be a positive integer");
        }
    }
}
