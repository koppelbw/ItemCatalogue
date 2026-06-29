using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

// Input rules for container writes. Limits mirror the EF Core column mappings in
// ItemCatalogueDbContext (Name nvarchar(100), Description nvarchar(500), Color nvarchar(9)). A
// container is owned by exactly one parent: either a Room (top-level) or another Container (nested) —
// never both, never neither. This XOR mirrors the CK_Container_RoomXorParent check constraint in the
// database. Placement is in inches: Position X/Y and Z are non-negative; Rotation is on [0, 360);
// size dimensions, when supplied, are positive. Color, when supplied, must be hex.
public sealed class CreateContainerRequestValidator : AbstractValidator<CreateContainerRequest>
{
    public CreateContainerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .When(x => x.RoomId.HasValue);

        RuleFor(x => x.ParentContainerId)
            .GreaterThan(0)
            .When(x => x.ParentContainerId.HasValue);

        RuleFor(x => x)
            .Must(x => x.RoomId.HasValue ^ x.ParentContainerId.HasValue)
            .WithMessage("Specify exactly one of RoomId or ParentContainerId.")
            .WithName("Owner");

        RuleFor(x => x.ContainerType)
            .IsInEnum()
            .When(x => x.ContainerType.HasValue);

        RuleFor(x => x.PositionXInches).GreaterThanOrEqualTo(0).When(x => x.PositionXInches.HasValue);
        RuleFor(x => x.PositionYInches).GreaterThanOrEqualTo(0).When(x => x.PositionYInches.HasValue);
        RuleFor(x => x.PositionZInches).GreaterThanOrEqualTo(0).When(x => x.PositionZInches.HasValue);
        RuleFor(x => x.Rotation).GreaterThanOrEqualTo(0).LessThan(GeometryRules.MaxRotationExclusive).When(x => x.Rotation.HasValue);
        RuleFor(x => x.WidthInches).GreaterThan(0).When(x => x.WidthInches.HasValue);
        RuleFor(x => x.DepthInches).GreaterThan(0).When(x => x.DepthInches.HasValue);
        RuleFor(x => x.HeightInches).GreaterThan(0).When(x => x.HeightInches.HasValue);

        RuleFor(x => x.Color).Matches(GeometryRules.HexColorPattern).When(x => !string.IsNullOrEmpty(x.Color));
    }
}

public sealed class UpdateContainerRequestValidator : AbstractValidator<UpdateContainerRequest>
{
    public UpdateContainerRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .When(x => x.RoomId.HasValue);

        RuleFor(x => x.ParentContainerId)
            .GreaterThan(0)
            .When(x => x.ParentContainerId.HasValue);

        RuleFor(x => x)
            .Must(x => x.RoomId.HasValue ^ x.ParentContainerId.HasValue)
            .WithMessage("Specify exactly one of RoomId or ParentContainerId.")
            .WithName("Owner");

        RuleFor(x => x.ContainerType)
            .IsInEnum()
            .When(x => x.ContainerType.HasValue);

        RuleFor(x => x.PositionXInches).GreaterThanOrEqualTo(0).When(x => x.PositionXInches.HasValue);
        RuleFor(x => x.PositionYInches).GreaterThanOrEqualTo(0).When(x => x.PositionYInches.HasValue);
        RuleFor(x => x.PositionZInches).GreaterThanOrEqualTo(0).When(x => x.PositionZInches.HasValue);
        RuleFor(x => x.Rotation).GreaterThanOrEqualTo(0).LessThan(GeometryRules.MaxRotationExclusive).When(x => x.Rotation.HasValue);
        RuleFor(x => x.WidthInches).GreaterThan(0).When(x => x.WidthInches.HasValue);
        RuleFor(x => x.DepthInches).GreaterThan(0).When(x => x.DepthInches.HasValue);
        RuleFor(x => x.HeightInches).GreaterThan(0).When(x => x.HeightInches.HasValue);

        RuleFor(x => x.Color).Matches(GeometryRules.HexColorPattern).When(x => !string.IsNullOrEmpty(x.Color));

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
