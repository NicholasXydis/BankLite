using BankLite.Application.DTOs;
using FluentValidation;

namespace BankLite.Application.Validators
{
    public class RegisterUserValidator : AbstractValidator<RegisterUserDto>
    {
        public RegisterUserValidator()
        {
            RuleFor(x => x.FullName).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress().Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$").WithMessage("Please enter a valid email address");
            RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        }
    }
}
