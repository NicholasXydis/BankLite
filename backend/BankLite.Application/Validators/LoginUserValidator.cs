using BankLite.Application.DTOs;
using FluentValidation;

namespace BankLite.Application.Validators;

public class LoginUserValidator : AbstractValidator<LoginUserDto>
{
    public LoginUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().Matches(EmailRules.Pattern)
            .WithMessage("Please enter a valid email address").MaximumLength(EmailRules.MaxLength);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(100);
    }
}