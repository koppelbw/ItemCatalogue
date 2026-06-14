using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class CreateTagRequestValidatorTests
{
    private readonly CreateTagRequestValidator _validator = new();

    private static CreateTagRequest Valid() => new(Name: "Fragile", Description: "Handle with care");

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { Name = "" }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Name_LongerThan100_IsRejected() =>
        _validator.TestValidate(Valid() with { Name = new string('a', 101) }).ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Description_LongerThan500_IsRejected() =>
        _validator.TestValidate(Valid() with { Description = new string('a', 501) }).ShouldHaveValidationErrorFor(x => x.Description);
}

public class UpdateTagRequestValidatorTests
{
    private readonly UpdateTagRequestValidator _validator = new();

    private static UpdateTagRequest Valid() => new(Id: 1, Name: "Fragile", Description: null, RowVersion: [1, 2, 3]);

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

public class SetItemTagsRequestValidatorTests
{
    private readonly SetItemTagsRequestValidator _validator = new();

    [Fact]
    public void EmptyList_IsAllowed() =>
        _validator.TestValidate(new SetItemTagsRequest([])).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void PositiveIds_HaveNoErrors() =>
        _validator.TestValidate(new SetItemTagsRequest([1, 2, 3])).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NonPositiveId_IsRejected() =>
        _validator.TestValidate(new SetItemTagsRequest([1, 0])).ShouldHaveValidationErrorFor("TagIds[1]");
}
