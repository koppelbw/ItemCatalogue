using Application.DTOs;
using Application.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class UpdateItemRequestValidatorTests
{
    private readonly UpdateItemRequestValidator _validator = new();

    private static UpdateItemRequest Valid() =>
        new(Id: 1,
            Name: "Desk Lamp",
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
            OwnerId: null,
            ReleaseDate: null,
            ValuationDate: null,
            AcquisitionReference: null,
            RowVersion: [1, 2, 3, 4]);

    [Fact]
    public void ValidRequest_HasNoErrors()
    {
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Id_WhenNotPositive_IsRejected()
    {
        _validator.TestValidate(Valid() with { Id = 0 })
            .ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void RowVersion_WhenEmpty_IsRejected()
    {
        _validator.TestValidate(Valid() with { RowVersion = [] })
            .ShouldHaveValidationErrorFor(x => x.RowVersion);
    }

    [Fact]
    public void PurchaseDate_BeforeReleaseDate_IsRejected()
    {
        _validator.TestValidate(Valid() with
        {
            ReleaseDate = new DateTime(2025, 1, 1),
            PurchaseDate = new DateTime(2024, 1, 1),
        }).ShouldHaveValidationErrorFor("PurchaseDate");
    }

    [Fact]
    public void AcquisitionReference_LongerThan100_IsRejected()
    {
        _validator.TestValidate(Valid() with { AcquisitionReference = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.AcquisitionReference);
    }
}
