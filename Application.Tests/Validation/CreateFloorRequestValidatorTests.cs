using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class CreateFloorRequestValidatorTests
{
    private readonly CreateFloorRequestValidator _validator = new();

    private static CreateFloorRequest Valid() =>
        new(Name: "First Floor", LocationId: 3, LevelIndex: 0, ElevationInches: null, CeilingHeightInches: 96);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { Name = "" })
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void LocationId_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { LocationId = 0 })
            .ShouldHaveValidationErrorFor(x => x.LocationId);

    [Fact]
    public void NegativeLevelIndex_IsAllowed() =>
        _validator.TestValidate(Valid() with { LevelIndex = -1 })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void CeilingHeight_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { CeilingHeightInches = 0 })
            .ShouldHaveValidationErrorFor(x => x.CeilingHeightInches);
}
