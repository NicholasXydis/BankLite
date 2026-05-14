using BankLite.Application.DTOs;
using FluentValidation;

namespace BankLite.Application.Validators
{
    public class DepositWithdrawValidator : AbstractValidator<DepositWithdrawDto>
    {
        public DepositWithdrawValidator()
        {
            RuleFor(x => x.Amount).GreaterThan(0).LessThanOrEqualTo(1000000);
            RuleFor(x => x.AccountId).NotEmpty();
        }
    }
}
