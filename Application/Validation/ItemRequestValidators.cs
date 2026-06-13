using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

// Input rules for item writes. Limits mirror the EF Core column mappings in
// ItemCatalogueDbContext (Name nvarchar(255), Description nvarchar(4000), Price decimal(18,2))
// so bad input is rejected at the boundary with a 400 instead of failing deep in EF/SQL.
public sealed class CreateItemRequestValidator : AbstractValidator<CreateItemRequest>
{
    public CreateItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .MaximumLength(4000);

        // ItemTypes is stored as a required JSON column; an item must carry at least one type.
        RuleFor(x => x.ItemTypes)
            .NotEmpty();

        RuleForEach(x => x.ItemTypes)
            .IsInEnum();

        // decimal(18,2): non-negative and within the two-decimal-place scale of the column.
        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .When(x => x.Price.HasValue);

        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .When(x => x.RoomId.HasValue);

        RuleFor(x => x.OwnerId)
            .GreaterThan(0)
            .When(x => x.OwnerId.HasValue);
    }
}

public sealed class UpdateItemRequestValidator : AbstractValidator<UpdateItemRequest>
{
    public UpdateItemRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(255);

        RuleFor(x => x.Description)
            .MaximumLength(4000);

        RuleFor(x => x.ItemTypes)
            .NotEmpty();

        RuleForEach(x => x.ItemTypes)
            .IsInEnum();

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .When(x => x.Price.HasValue);

        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .When(x => x.RoomId.HasValue);

        RuleFor(x => x.OwnerId)
            .GreaterThan(0)
            .When(x => x.OwnerId.HasValue);

        // The optimistic-concurrency token must be supplied on update so the repository can
        // detect a stale write; an empty token would defeat the rowversion check.
        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
