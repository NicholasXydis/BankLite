using BankLite.Application.DTOs;
using FluentValidation;

namespace BankLite.Application.Validators;

public class RegisterUserValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .MaximumLength(50)
            .Must(x => x == x.Trim()).WithMessage("Full name cannot have leading or trailing spaces")
            .Must(x => x.All(c => char.IsLetter(c) || char.IsWhiteSpace(c)))
            .WithMessage("Full name can only contain letters and spaces.");
        RuleFor(x => x.Email).NotEmpty().Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")
            .WithMessage("Please enter a valid email address").MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}