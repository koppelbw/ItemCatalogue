using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

// Input rules for room writes. Limits mirror the EF Core column mappings in ItemCatalogueDbContext
// (Name nvarchar(100), Description nvarchar(500), colours nvarchar(9)). Geometry is optional (a room
// may exist before it is measured), but each supplied value must be sensible: positive width/depth,
// non-negative origins, rotation on [0, 360), and hex colours ("#RRGGBB" or "#RRGGBBAA").
public sealed class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.FloorId)
            .GreaterThan(0);

        RuleFor(x => x.RoomType).IsInEnum().When(x => x.RoomType.HasValue);

        RuleFor(x => x.OriginXInches).GreaterThanOrEqualTo(0).When(x => x.OriginXInches.HasValue);
        RuleFor(x => x.OriginYInches).GreaterThanOrEqualTo(0).When(x => x.OriginYInches.HasValue);
        RuleFor(x => x.WidthInches).GreaterThan(0).When(x => x.WidthInches.HasValue);
        RuleFor(x => x.DepthInches).GreaterThan(0).When(x => x.DepthInches.HasValue);
        RuleFor(x => x.HeightInches).GreaterThan(0).When(x => x.HeightInches.HasValue);
        RuleFor(x => x.Rotation).GreaterThanOrEqualTo(0).LessThan(GeometryRules.MaxRotationExclusive).When(x => x.Rotation.HasValue);

        RuleFor(x => x.WallColor).Matches(GeometryRules.HexColorPattern).When(x => !string.IsNullOrEmpty(x.WallColor));
        RuleFor(x => x.FloorColor).Matches(GeometryRules.HexColorPattern).When(x => !string.IsNullOrEmpty(x.FloorColor));
        RuleFor(x => x.CeilingColor).Matches(GeometryRules.HexColorPattern).When(x => !string.IsNullOrEmpty(x.CeilingColor));
    }
}

public sealed class UpdateRoomRequestValidator : AbstractValidator<UpdateRoomRequest>
{
    public UpdateRoomRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.FloorId)
            .GreaterThan(0);

        RuleFor(x => x.RoomType).IsInEnum().When(x => x.RoomType.HasValue);

        RuleFor(x => x.OriginXInches).GreaterThanOrEqualTo(0).When(x => x.OriginXInches.HasValue);
        RuleFor(x => x.OriginYInches).GreaterThanOrEqualTo(0).When(x => x.OriginYInches.HasValue);
        RuleFor(x => x.WidthInches).GreaterThan(0).When(x => x.WidthInches.HasValue);
        RuleFor(x => x.DepthInches).GreaterThan(0).When(x => x.DepthInches.HasValue);
        RuleFor(x => x.HeightInches).GreaterThan(0).When(x => x.HeightInches.HasValue);
        RuleFor(x => x.Rotation).GreaterThanOrEqualTo(0).LessThan(GeometryRules.MaxRotationExclusive).When(x => x.Rotation.HasValue);

        RuleFor(x => x.WallColor).Matches(GeometryRules.HexColorPattern).When(x => !string.IsNullOrEmpty(x.WallColor));
        RuleFor(x => x.FloorColor).Matches(GeometryRules.HexColorPattern).When(x => !string.IsNullOrEmpty(x.FloorColor));
        RuleFor(x => x.CeilingColor).Matches(GeometryRules.HexColorPattern).When(x => !string.IsNullOrEmpty(x.CeilingColor));

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
