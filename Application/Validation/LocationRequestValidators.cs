using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

// Input rules for location writes. Limits mirror the EF Core column mappings in
// ItemCatalogueDbContext (Name nvarchar(100), Description nvarchar(500)); RoomId is a
// required foreign key.
public sealed class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.RoomId)
            .GreaterThan(0);
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

        RuleFor(x => x.RoomId)
            .GreaterThan(0);

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
