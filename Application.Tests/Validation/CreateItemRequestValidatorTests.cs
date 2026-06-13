using Application.DTOs;
using Application.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class CreateItemRequestValidatorTests
{
    private readonly CreateItemRequestValidator _validator = new();

    // A request that satisfies every rule; individual tests mutate one field to prove a rule.
    private static CreateItemRequest Valid() =>
        new(Name: "Desk Lamp",
            Description: "A small lamp",
            ItemTypes: [ItemType.Electronics],
            PurchasePrice: 19.99m,
            CurrentValue: null,
            Brand: null,
            Model: null,
            SerialNumber: null,
            PurchasedFrom: null,
            Quantity: 1,
            Condition: null,
            AcquisitionType: null,
            PurchaseDate: null,
            WarrantyExpiryDate: null,
            IsStored: false,
            RoomId: null,
            ContainerId: null,
            OwnerId: null);

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Name_WhenEmpty_IsRejected(string? name)
    {
        _validator.TestValidate(Valid() with { Name = name! })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_LongerThan255_IsRejected()
    {
        _validator.TestValidate(Valid() with { Name = new string('a', 256) })
            .ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Description_LongerThan4000_IsRejected()
    {
        _validator.TestValidate(Valid() with { Description = new string('a', 4001) })
            .ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ItemTypes_WhenEmpty_IsRejected()
    {
        _validator.TestValidate(Valid() with { ItemTypes = [] })
            .ShouldHaveValidationErrorFor(x => x.ItemTypes);
    }

    [Fact]
    public void ItemTypes_WithUndefinedEnumValue_IsRejected()
    {
        _validator.TestValidate(Valid() with { ItemTypes = [(ItemType)999] })
            .ShouldHaveValidationErrorFor("ItemTypes[0]");
    }

    [Fact]
    public void PurchasePrice_WhenNegative_IsRejected()
    {
        _validator.TestValidate(Valid() with { PurchasePrice = -0.01m })
            .ShouldHaveValidationErrorFor(x => x.PurchasePrice);
    }

    [Fact]
    public void PurchasePrice_WithMoreThanTwoDecimalPlaces_IsRejected()
    {
        _validator.TestValidate(Valid() with { PurchasePrice = 1.234m })
            .ShouldHaveValidationErrorFor(x => x.PurchasePrice);
    }

    [Fact]
    public void PurchasePrice_WhenNull_IsAllowed()
    {
        _validator.TestValidate(Valid() with { PurchasePrice = null })
            .ShouldNotHaveValidationErrorFor(x => x.PurchasePrice);
    }

    [Fact]
    public void CurrentValue_WhenNegative_IsRejected()
    {
        _validator.TestValidate(Valid() with { CurrentValue = -0.01m })
            .ShouldHaveValidationErrorFor(x => x.CurrentValue);
    }

    [Fact]
    public void CurrentValue_MayExceedPurchasePrice()
    {
        // Items can appreciate, so a current value above the purchase price is allowed.
        _validator.TestValidate(Valid() with { PurchasePrice = 10m, CurrentValue = 5000m })
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RoomId_WhenZero_IsRejected()
    {
        _validator.TestValidate(Valid() with { RoomId = 0 })
            .ShouldHaveValidationErrorFor(x => x.RoomId);
    }

    [Fact]
    public void ContainerId_WhenZero_IsRejected()
    {
        _validator.TestValidate(Valid() with { ContainerId = 0 })
            .ShouldHaveValidationErrorFor(x => x.ContainerId);
    }

    [Fact]
    public void RoomAndContainer_BothSet_IsRejected()
    {
        // An item lives in a Room or a Container, not both.
        _validator.TestValidate(Valid() with { RoomId = 1, ContainerId = 2 })
            .ShouldHaveValidationErrorFor("Placement");
    }

    [Fact]
    public void RoomOrContainer_OnlyOneSet_IsAllowed()
    {
        _validator.TestValidate(Valid() with { ContainerId = 2 }).ShouldNotHaveAnyValidationErrors();
        _validator.TestValidate(Valid() with { RoomId = 1 }).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void OwnerId_WhenNegative_IsRejected()
    {
        _validator.TestValidate(Valid() with { OwnerId = -1 })
            .ShouldHaveValidationErrorFor(x => x.OwnerId);
    }

    [Fact]
    public void Brand_LongerThan100_IsRejected()
    {
        _validator.TestValidate(Valid() with { Brand = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.Brand);
    }

    [Fact]
    public void SerialNumber_LongerThan100_IsRejected()
    {
        _validator.TestValidate(Valid() with { SerialNumber = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.SerialNumber);
    }

    [Fact]
    public void Quantity_WhenLessThanOne_IsRejected()
    {
        _validator.TestValidate(Valid() with { Quantity = 0 })
            .ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void Condition_WithUndefinedEnumValue_IsRejected()
    {
        _validator.TestValidate(Valid() with { Condition = (Condition)999 })
            .ShouldHaveValidationErrorFor(x => x.Condition);
    }

    [Fact]
    public void AcquisitionType_WithUndefinedEnumValue_IsRejected()
    {
        _validator.TestValidate(Valid() with { AcquisitionType = (AcquisitionType)999 })
            .ShouldHaveValidationErrorFor(x => x.AcquisitionType);
    }

    [Fact]
    public void WarrantyExpiry_BeforePurchaseDate_IsRejected()
    {
        _validator.TestValidate(Valid() with
        {
            PurchaseDate = new DateTime(2025, 1, 1),
            WarrantyExpiryDate = new DateTime(2024, 1, 1),
        }).ShouldHaveValidationErrorFor("WarrantyExpiryDate");
    }

    [Fact]
    public void WarrantyExpiry_AfterPurchaseDate_IsAllowed()
    {
        _validator.TestValidate(Valid() with
        {
            PurchaseDate = new DateTime(2024, 1, 1),
            WarrantyExpiryDate = new DateTime(2025, 1, 1),
        }).ShouldNotHaveAnyValidationErrors();
    }
}
