using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

public sealed class ChatRequestValidator : AbstractValidator<ChatRequest>
{
    // Caps guard cost as much as correctness: every turn in the history is re-sent to the model
    // (and billed) on every request.
    private const int MaxTurns = 40;
    private const int MaxTurnLength = 4000;

    public ChatRequestValidator()
    {
        RuleFor(x => x.Messages)
            .NotEmpty()
            .Must(m => m is null || m.Count <= MaxTurns)
                .WithMessage($"A conversation may contain at most {MaxTurns} turns; start a new chat.")
            .Must(m => m is not { Count: > 0 } || m[^1].Role == "user")
                .WithMessage("The last turn must be from the user.");

        RuleForEach(x => x.Messages).ChildRules(turn =>
        {
            turn.RuleFor(t => t.Role)
                .Must(r => r is "user" or "assistant")
                .WithMessage("Role must be 'user' or 'assistant'.");

            turn.RuleFor(t => t.Content)
                .NotEmpty()
                .MaximumLength(MaxTurnLength);
        });
    }
}
