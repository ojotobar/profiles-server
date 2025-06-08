using FluentValidation;
using ProfessionalProfiles.Graph.Educations;
namespace ProfessionalProfiles.Graph.Validations
{
    public class EducationInputValidator : AbstractValidator<EducationInput>
    {
        public EducationInputValidator()
        {
            RuleFor(x => x.SchoolName)
                .NotEmpty().WithMessage("Institution name is required");
            RuleFor(x => x.Level)
                .IsInEnum().WithMessage("Invalid education level");
            RuleFor(x => x.Course)
                .NotEmpty().WithMessage("Course is required.");
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
            RuleFor(x => x)
                .Must(args => ValidationExtensions.LevelSpecifiedWhenOther(args.Level, args.OtherLevelSpecification))
                .WithMessage("Education Level must be specified when 'Other' is selected");
        }
    }
}
