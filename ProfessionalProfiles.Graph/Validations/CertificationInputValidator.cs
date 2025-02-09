using FluentValidation;
using ProfessionalProfiles.Graph.Certfications;

namespace ProfessionalProfiles.Graph.Validations
{
    public class CertificationInputValidator : AbstractValidator<CertificationInput>
    {
        public CertificationInputValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Certification name is required");
            RuleFor(x => x.InstitutionName)
                .NotEmpty().WithMessage("Certifying Institution is required");
            RuleFor(x => x.Date)
                .NotEmpty().WithMessage("Certification Date is required.");
            RuleFor(x => x.YearsOfValidity)
                .Must(ValidationExtensions.BeAValidNumber).WithMessage("Years f validity must be greater than 0");
        }
    }
}
