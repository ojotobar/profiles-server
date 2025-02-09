using FluentValidation;
using ProfessionalProfiles.Graph.Projects;

namespace ProfessionalProfiles.Graph.Validations
{
    public class ProjectInputValidator : AbstractValidator<ProjectInput>
    {
        public ProjectInputValidator()
        {
            RuleFor(x => x.ProjectName)
                .NotEmpty().WithMessage("Project Name is required");
            RuleFor(x => x.Summary)
                .NotEmpty().WithMessage("Project Summary is required.");
            RuleFor(x => x.Technologies)
                .Must(ValidationExtensions.BeAValidListOfString).WithMessage("All specified technologies must be one or more characters.");
        }
    }
}
