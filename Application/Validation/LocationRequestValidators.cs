using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

// Input rules for location writes. Limits mirror the EF Core column mappings in
// ItemCatalogueDbContext (Name nvarchar(100), Description nvarchar(500)). A Location's
// Rooms are managed through the Room endpoints (Room.LocationId), not on the Location write.
public sealed class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

public sealed class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
