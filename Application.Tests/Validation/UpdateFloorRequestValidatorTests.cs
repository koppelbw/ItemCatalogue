using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class UpdateFloorRequestValidatorTests
{
    private readonly UpdateFloorRequestValidator _validator = new();

    private static UpdateFloorRequest Valid() =>
        new(Id: 1, Name: "First Floor", LocationId: 3, LevelIndex: 0, ElevationInches: null, CeilingHeightInches: 96, RowVersion: [1, 2, 3]);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Id_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { Id = 0 })
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void LocationId_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { LocationId = 0 })
            .ShouldHaveValidationErrorFor(x => x.LocationId);

    [Fact]
    public void RowVersion_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { RowVersion = [] })
            .ShouldHaveValidationErrorFor(x => x.RowVersion);
}
