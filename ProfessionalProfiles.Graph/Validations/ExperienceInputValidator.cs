using FluentValidation;
using ProfessionalProfiles.Graph.Experiences;

namespace ProfessionalProfiles.Graph.Validations
{
    public class ExperienceInputValidator : AbstractValidator<ExperienceInput>
    {
        public ExperienceInputValidator()
        {
            RuleFor(x => x.Organization)
                .NotEmpty().WithMessage("Organization name is required");
            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Job Title is required");
            RuleFor(x => x.Location)
                .NotNull().WithMessage("Location is required");
            RuleFor(x => x.Location.City)
                .NotEmpty().WithMessage("City is required");
            RuleFor(x => x.Location.Country)
                .NotEmpty().WithMessage("Country is required");
            RuleFor(x => x.StartDate)
                .Must(ValidationExtensions.BeAValidDate).WithMessage("Start Date is required");
            RuleFor(x => x.EndDate)
                .Must(ValidationExtensions.BeAValidDate).WithMessage("Invalid End Date");
            RuleFor(x => x).Must(args => ValidationExtensions.BeAValidDateRange(args.StartDate, args.EndDate))
                .WithMessage("End Date must be later than the Start Date.");
            RuleFor(x => x.Summaries)
                .Must(ValidationExtensions.BeAValidListOfString).WithMessage("All job description entries must be one or more characters long.");
        }
    }
}
