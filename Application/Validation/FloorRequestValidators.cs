using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

// Input rules for floor writes. Name limit mirrors the EF Core column mapping (nvarchar(100)).
// LevelIndex is unconstrained (basements are negative). ElevationInches is unconstrained (a
// below-grade floor sits below the datum); CeilingHeightInches must be positive when supplied.
public sealed class CreateFloorRequestValidator : AbstractValidator<CreateFloorRequest>
{
    public CreateFloorRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LocationId)
            .GreaterThan(0);

        RuleFor(x => x.CeilingHeightInches)
            .GreaterThan(0)
            .When(x => x.CeilingHeightInches.HasValue);
    }
}

public sealed class UpdateFloorRequestValidator : AbstractValidator<UpdateFloorRequest>
{
    public UpdateFloorRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.LocationId)
            .GreaterThan(0);

        RuleFor(x => x.CeilingHeightInches)
            .GreaterThan(0)
            .When(x => x.CeilingHeightInches.HasValue);

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
