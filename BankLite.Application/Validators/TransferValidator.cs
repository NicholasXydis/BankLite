using BankLite.Application.DTOs;
using FluentValidation;

namespace BankLite.Application.Validators
{
    public class TransferValidator : AbstractValidator<TransferDto>
    {
        public TransferValidator()
        {
            RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1000000);
            RuleFor(x => x.FromAccountId).NotEmpty();
            RuleFor(x => x.ToAccountId).NotEmpty().NotEqual(x => x.FromAccountId).WithMessage("Cannot transfer to the same account.");
        }
    }
}
