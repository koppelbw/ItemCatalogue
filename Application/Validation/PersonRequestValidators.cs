using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

// Input rules for person writes. Limit mirrors the EF Core column mapping in
// ItemCatalogueDbContext (Name nvarchar(100)).
public sealed class CreatePersonRequestValidator : AbstractValidator<CreatePersonRequest>
{
    public CreatePersonRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}

public sealed class UpdatePersonRequestValidator : AbstractValidator<UpdatePersonRequest>
{
    public UpdatePersonRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
