using BankLite.Application.DTOs;
using FluentValidation;

namespace BankLite.Application.Validators;

public class CreateAccountValidator : AbstractValidator<CreateAccountDto>
{
    public CreateAccountValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
    }
}