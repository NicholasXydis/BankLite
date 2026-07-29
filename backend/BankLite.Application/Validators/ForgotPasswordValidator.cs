using BankLite.Application.DTOs;
using FluentValidation;

namespace BankLite.Application.Validators;

public class ForgotPasswordValidator : AbstractValidator<ForgotPasswordDto>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .Matches(EmailRules.Pattern).WithMessage("Please enter a valid email address.")
            .MaximumLength(EmailRules.MaxLength);
    }
}