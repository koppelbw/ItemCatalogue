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
            Price: 19.99m,
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
    public void Price_WhenNegative_IsRejected()
    {
        _validator.TestValidate(Valid() with { Price = -0.01m })
            .ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Price_WithMoreThanTwoDecimalPlaces_IsRejected()
    {
        _validator.TestValidate(Valid() with { Price = 1.234m })
            .ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Price_WhenNull_IsAllowed()
    {
        _validator.TestValidate(Valid() with { Price = null })
            .ShouldNotHaveValidationErrorFor(x => x.Price);
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
}
