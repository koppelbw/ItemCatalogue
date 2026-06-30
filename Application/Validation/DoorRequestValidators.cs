using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

// Input rules for door writes. FromRoomId is required; ToRoomId is optional (null = leads outside)
// and, when set, must differ from FromRoomId (a door cannot connect a room to itself). Offset is a
// distance along the wall (>= 0); the opening must have positive width and height. The DB backstops
// the self-connection with CK_Door_FromNotEqualTo.
public sealed class CreateDoorRequestValidator : AbstractValidator<CreateDoorRequest>
{
    public CreateDoorRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100);

        RuleFor(x => x.Kind)
            .IsInEnum();

        RuleFor(x => x.Wall)
            .IsInEnum();

        RuleFor(x => x.FromRoomId)
            .GreaterThan(0);

        RuleFor(x => x.ToRoomId)
            .GreaterThan(0)
            .When(x => x.ToRoomId.HasValue);

        RuleFor(x => x.ToRoomId)
            .NotEqual(x => x.FromRoomId)
            .When(x => x.ToRoomId.HasValue)
            .WithMessage("A door cannot connect a room to itself.");

        RuleFor(x => x.OffsetInches)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.WidthInches)
            .GreaterThan(0);

        RuleFor(x => x.HeightInches)
            .GreaterThan(0);

        RuleFor(x => x.HingeSide)
            .IsInEnum()
            .When(x => x.HingeSide.HasValue);

        RuleFor(x => x.Swing)
            .IsInEnum()
            .When(x => x.Swing.HasValue);
    }
}

public sealed class UpdateDoorRequestValidator : AbstractValidator<UpdateDoorRequest>
{
    public UpdateDoorRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .MaximumLength(100);

        RuleFor(x => x.Kind)
            .IsInEnum();

        RuleFor(x => x.Wall)
            .IsInEnum();

        RuleFor(x => x.FromRoomId)
            .GreaterThan(0);

        RuleFor(x => x.ToRoomId)
            .GreaterThan(0)
            .When(x => x.ToRoomId.HasValue);

        RuleFor(x => x.ToRoomId)
            .NotEqual(x => x.FromRoomId)
            .When(x => x.ToRoomId.HasValue)
            .WithMessage("A door cannot connect a room to itself.");

        RuleFor(x => x.OffsetInches)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.WidthInches)
            .GreaterThan(0);

        RuleFor(x => x.HeightInches)
            .GreaterThan(0);

        RuleFor(x => x.HingeSide)
            .IsInEnum()
            .When(x => x.HingeSide.HasValue);

        RuleFor(x => x.Swing)
            .IsInEnum()
            .When(x => x.Swing.HasValue);

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
