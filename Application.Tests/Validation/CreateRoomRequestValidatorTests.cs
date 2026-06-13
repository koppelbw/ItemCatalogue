using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class CreateRoomRequestValidatorTests
{
    private readonly CreateRoomRequestValidator _validator = new();

    private static CreateRoomRequest Valid() => new(Name: "Garage", Description: "Out back", LocationId: 3);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { Name = "" })
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Name_LongerThan100_IsRejected() =>
        _validator.TestValidate(Valid() with { Name = new string('a', 101) })
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Description_LongerThan500_IsRejected() =>
        _validator.TestValidate(Valid() with { Description = new string('a', 501) })
            .ShouldHaveValidationErrorFor(x => x.Description);

    [Fact]
    public void Description_WhenNull_IsAllowed() =>
        _validator.TestValidate(Valid() with { Description = null })
            .ShouldNotHaveValidationErrorFor(x => x.Description);

    [Fact]
    public void LocationId_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { LocationId = 0 })
            .ShouldHaveValidationErrorFor(x => x.LocationId);
}
