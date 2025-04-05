using FluentValidation;
using ProfessionalProfiles.Graph.Account;

namespace ProfessionalProfiles.Graph.Validations
{
    public class ProfileDetailsInputValidator : AbstractValidator<ProfileDetailsInput>
    {
        public ProfileDetailsInputValidator()
        {
            RuleFor(p => p.FirstName)
                .NotEmpty().WithMessage("First Name is required.");
            RuleFor(p => p.LastName)
                .NotEmpty().WithMessage("Last Name is required.");
            RuleFor(p => p.Phone)
                .NotEmpty().WithMessage("Phone Number is required.");
            RuleFor(s => s.Gender)
                .IsInEnum().WithMessage("Please enter a valid gender");
        }
    }
}
