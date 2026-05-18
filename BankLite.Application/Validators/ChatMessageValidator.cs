using BankLite.Application.DTOs;
using FluentValidation;

namespace BankLite.Application.Validators
{
    public class ChatMessageValidator : AbstractValidator<ChatMessageDto>
    {
        public ChatMessageValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty().WithMessage("Message cannot be empty.")
                .MaximumLength(200).WithMessage("Message cannot exceed 200 characters.");
        }
    }
}