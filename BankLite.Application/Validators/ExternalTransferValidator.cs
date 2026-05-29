using BankLite.Application.DTOs;
using FluentValidation;

namespace BankLite.Application.Validators;

public class ExternalTransferValidator : AbstractValidator<ExternalTransferDto>
{
    public ExternalTransferValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1000000);
        RuleFor(x => x.FromAccountId).NotEmpty();
        RuleFor(x => x.ToAccountNumber).NotEmpty().WithMessage("Please enter an account number.").Length(12)
            .WithMessage("Account number must be 12 characters.");
    }
}