using FluentValidation;
using ProfessionalProfiles.Graph.Account;

namespace ProfessionalProfiles.Graph.Validations
{
    public class UserLocationInputValidator : AbstractValidator<UserLocationInput>
    {
        public UserLocationInputValidator()
        {
            RuleFor(x => x.Line1)
                .NotEmpty().WithMessage("{PropertyName} field is required.");
            RuleFor(x => x.PostalCode)
                .NotEmpty().WithMessage("{PropertyName} field is required.");
            RuleFor(x => x.Country)
                .NotEmpty().WithMessage("{PropertyName} field is required.");
            RuleFor(x => x.State)
                .NotEmpty().WithMessage("{PropertyName} field is required.");
            RuleFor(x => x.City)
                .NotEmpty().WithMessage("{PropertyName} field is required.");
        }
    }
}
