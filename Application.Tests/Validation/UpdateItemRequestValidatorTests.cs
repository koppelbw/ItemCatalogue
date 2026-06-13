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
            Price: 19.99m,
            IsStored: false,
            RoomId: null,
            ContainerId: null,
            OwnerId: null,
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
}
