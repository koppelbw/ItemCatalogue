using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

// Input rules for room writes. Limits mirror the EF Core column mappings in
// ItemCatalogueDbContext (Name nvarchar(100), Description nvarchar(500)).
public sealed class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.LocationId)
            .GreaterThan(0);
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

        RuleFor(x => x.LocationId)
            .GreaterThan(0);

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
