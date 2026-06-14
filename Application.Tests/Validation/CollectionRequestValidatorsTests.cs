using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class CreateCollectionRequestValidatorTests
{
    private readonly CreateCollectionRequestValidator _validator = new();

    private static CreateCollectionRequest Valid() => new(Name: "Office Kit", Description: "Desk setup");

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Name_LongerThan100_IsRejected() =>
        _validator.TestValidate(Valid() with { Name = new string('a', 101) }).ShouldHaveValidationErrorFor(x => x.Name);
}

public class UpdateCollectionRequestValidatorTests
{
    private readonly UpdateCollectionRequestValidator _validator = new();

    private static UpdateCollectionRequest Valid() => new(Id: 1, Name: "Office Kit", Description: null, RowVersion: [1, 2, 3]);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Id_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { Id = 0 }).ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void RowVersion_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { RowVersion = [] }).ShouldHaveValidationErrorFor(x => x.RowVersion);
}

public class AddCollectionItemRequestValidatorTests
{
    private readonly AddCollectionItemRequestValidator _validator = new();

    private static AddCollectionItemRequest Valid() => new(ItemId: 7, Quantity: 1, SortOrder: 0, Role: "Primary");

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NullSortOrder_IsAllowed() =>
        _validator.TestValidate(Valid() with { SortOrder = null }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void ItemId_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { ItemId = 0 }).ShouldHaveValidationErrorFor(x => x.ItemId);

    [Fact]
    public void Quantity_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { Quantity = 0 }).ShouldHaveValidationErrorFor(x => x.Quantity);

    [Fact]
    public void SortOrder_WhenNegative_IsRejected() =>
        _validator.TestValidate(Valid() with { SortOrder = -1 }).ShouldHaveValidationErrorFor(x => x.SortOrder);

    [Fact]
    public void Role_LongerThan100_IsRejected() =>
        _validator.TestValidate(Valid() with { Role = new string('a', 101) }).ShouldHaveValidationErrorFor(x => x.Role);
}

public class UpdateCollectionItemRequestValidatorTests
{
    private readonly UpdateCollectionItemRequestValidator _validator = new();

    private static UpdateCollectionItemRequest Valid() => new(Quantity: 1, SortOrder: 0, Role: "Primary");

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Quantity_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { Quantity = 0 }).ShouldHaveValidationErrorFor(x => x.Quantity);

    [Fact]
    public void SortOrder_WhenNegative_IsRejected() =>
        _validator.TestValidate(Valid() with { SortOrder = -1 }).ShouldHaveValidationErrorFor(x => x.SortOrder);
}
