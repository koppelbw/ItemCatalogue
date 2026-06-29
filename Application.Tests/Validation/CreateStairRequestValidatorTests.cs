using Application.DTOs;
using Application.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class CreateStairRequestValidatorTests
{
    private readonly CreateStairRequestValidator _validator = new();

    private static CreateStairRequest Valid() =>
        new(Name: "Basement Stairs", FromRoomId: 1, ToRoomId: 2, Shape: StairShape.Straight);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void LeadsToExterior_NullToRoom_IsAllowed() =>
        _validator.TestValidate(Valid() with { ToRoomId = null })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void FromRoomId_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { FromRoomId = 0 })
            .ShouldHaveValidationErrorFor(x => x.FromRoomId);

    [Fact]
    public void ToRoomId_EqualToFromRoomId_IsRejected() =>
        _validator.TestValidate(Valid() with { FromRoomId = 1, ToRoomId = 1 })
            .ShouldHaveValidationErrorFor(x => x.ToRoomId);

    [Fact]
    public void RiseInches_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { RiseInches = 0 })
            .ShouldHaveValidationErrorFor(x => x.RiseInches);

    [Fact]
    public void StepCount_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { StepCount = 0 })
            .ShouldHaveValidationErrorFor(x => x.StepCount);
}
