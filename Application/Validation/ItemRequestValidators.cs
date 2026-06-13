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
        // CurrentValue may exceed PurchasePrice (items can appreciate), so there is no cross-field rule.
        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .When(x => x.PurchasePrice.HasValue);

        RuleFor(x => x.CurrentValue)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .When(x => x.CurrentValue.HasValue);

        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .When(x => x.RoomId.HasValue);

        RuleFor(x => x.ContainerId)
            .GreaterThan(0)
            .When(x => x.ContainerId.HasValue);

        // An item lives either directly in a Room or inside a Container, not both.
        RuleFor(x => x)
            .Must(x => !(x.RoomId.HasValue && x.ContainerId.HasValue))
            .WithMessage("An item cannot reference both a Room and a Container.")
            .WithName("Placement");

        RuleFor(x => x.OwnerId)
            .GreaterThan(0)
            .When(x => x.OwnerId.HasValue);

        RuleFor(x => x.Brand)
            .MaximumLength(100);

        RuleFor(x => x.Model)
            .MaximumLength(100);

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100);

        RuleFor(x => x.PurchasedFrom)
            .MaximumLength(150);

        RuleFor(x => x.AcquisitionReference)
            .MaximumLength(100);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.Condition)
            .IsInEnum()
            .When(x => x.Condition.HasValue);

        RuleFor(x => x.AcquisitionType)
            .IsInEnum()
            .When(x => x.AcquisitionType.HasValue);

        RuleFor(x => x)
            .Must(x => x.WarrantyExpiryDate >= x.PurchaseDate)
            .WithMessage("WarrantyExpiryDate cannot be earlier than PurchaseDate.")
            .WithName("WarrantyExpiryDate")
            .When(x => x.PurchaseDate.HasValue && x.WarrantyExpiryDate.HasValue);

        RuleFor(x => x)
            .Must(x => x.PurchaseDate >= x.ReleaseDate)
            .WithMessage("PurchaseDate cannot be earlier than ReleaseDate.")
            .WithName("PurchaseDate")
            .When(x => x.PurchaseDate.HasValue && x.ReleaseDate.HasValue);
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

        // CurrentValue may exceed PurchasePrice (items can appreciate), so there is no cross-field rule.
        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .When(x => x.PurchasePrice.HasValue);

        RuleFor(x => x.CurrentValue)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, ignoreTrailingZeros: true)
            .When(x => x.CurrentValue.HasValue);

        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .When(x => x.RoomId.HasValue);

        RuleFor(x => x.ContainerId)
            .GreaterThan(0)
            .When(x => x.ContainerId.HasValue);

        // An item lives either directly in a Room or inside a Container, not both.
        RuleFor(x => x)
            .Must(x => !(x.RoomId.HasValue && x.ContainerId.HasValue))
            .WithMessage("An item cannot reference both a Room and a Container.")
            .WithName("Placement");

        RuleFor(x => x.OwnerId)
            .GreaterThan(0)
            .When(x => x.OwnerId.HasValue);

        RuleFor(x => x.Brand)
            .MaximumLength(100);

        RuleFor(x => x.Model)
            .MaximumLength(100);

        RuleFor(x => x.SerialNumber)
            .MaximumLength(100);

        RuleFor(x => x.PurchasedFrom)
            .MaximumLength(150);

        RuleFor(x => x.AcquisitionReference)
            .MaximumLength(100);

        RuleFor(x => x.Quantity)
            .GreaterThanOrEqualTo(1);

        RuleFor(x => x.Condition)
            .IsInEnum()
            .When(x => x.Condition.HasValue);

        RuleFor(x => x.AcquisitionType)
            .IsInEnum()
            .When(x => x.AcquisitionType.HasValue);

        RuleFor(x => x)
            .Must(x => x.WarrantyExpiryDate >= x.PurchaseDate)
            .WithMessage("WarrantyExpiryDate cannot be earlier than PurchaseDate.")
            .WithName("WarrantyExpiryDate")
            .When(x => x.PurchaseDate.HasValue && x.WarrantyExpiryDate.HasValue);

        RuleFor(x => x)
            .Must(x => x.PurchaseDate >= x.ReleaseDate)
            .WithMessage("PurchaseDate cannot be earlier than ReleaseDate.")
            .WithName("PurchaseDate")
            .When(x => x.PurchaseDate.HasValue && x.ReleaseDate.HasValue);

        // The optimistic-concurrency token must be supplied on update so the repository can
        // detect a stale write; an empty token would defeat the rowversion check.
        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
