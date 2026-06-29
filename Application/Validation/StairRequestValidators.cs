using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

public sealed class CreateStairRequestValidator : AbstractValidator<CreateStairRequest>
{
    public CreateStairRequestValidator()
    {
        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.Shape).IsInEnum();
        RuleFor(x => x.FromRoomId).GreaterThan(0);
        RuleFor(x => x.ToRoomId).GreaterThan(0).When(x => x.ToRoomId.HasValue);
        RuleFor(x => x.ToRoomId).NotEqual(x => x.FromRoomId).When(x => x.ToRoomId.HasValue)
            .WithMessage("A stair cannot connect a room to itself.");

        RuleFor(x => x.PositionXInches).GreaterThanOrEqualTo(0).When(x => x.PositionXInches.HasValue);
        RuleFor(x => x.PositionYInches).GreaterThanOrEqualTo(0).When(x => x.PositionYInches.HasValue);
        RuleFor(x => x.Rotation).GreaterThanOrEqualTo(0).LessThan(GeometryRules.MaxRotationExclusive).When(x => x.Rotation.HasValue);
        RuleFor(x => x.RunInches).GreaterThan(0).When(x => x.RunInches.HasValue);
        RuleFor(x => x.WidthInches).GreaterThan(0).When(x => x.WidthInches.HasValue);
        RuleFor(x => x.RiseInches).GreaterThan(0).When(x => x.RiseInches.HasValue);
        RuleFor(x => x.StepCount).GreaterThan(0).When(x => x.StepCount.HasValue);
    }
}

public sealed class UpdateStairRequestValidator : AbstractValidator<UpdateStairRequest>
{
    public UpdateStairRequestValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Name).MaximumLength(100);
        RuleFor(x => x.Shape).IsInEnum();
        RuleFor(x => x.FromRoomId).GreaterThan(0);
        RuleFor(x => x.ToRoomId).GreaterThan(0).When(x => x.ToRoomId.HasValue);
        RuleFor(x => x.ToRoomId).NotEqual(x => x.FromRoomId).When(x => x.ToRoomId.HasValue)
            .WithMessage("A stair cannot connect a room to itself.");

        RuleFor(x => x.PositionXInches).GreaterThanOrEqualTo(0).When(x => x.PositionXInches.HasValue);
        RuleFor(x => x.PositionYInches).GreaterThanOrEqualTo(0).When(x => x.PositionYInches.HasValue);
        RuleFor(x => x.Rotation).GreaterThanOrEqualTo(0).LessThan(GeometryRules.MaxRotationExclusive).When(x => x.Rotation.HasValue);
        RuleFor(x => x.RunInches).GreaterThan(0).When(x => x.RunInches.HasValue);
        RuleFor(x => x.WidthInches).GreaterThan(0).When(x => x.WidthInches.HasValue);
        RuleFor(x => x.RiseInches).GreaterThan(0).When(x => x.RiseInches.HasValue);
        RuleFor(x => x.StepCount).GreaterThan(0).When(x => x.StepCount.HasValue);

        RuleFor(x => x.RowVersion).NotEmpty();
    }
}
