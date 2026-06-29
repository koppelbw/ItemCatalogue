using Application.DTOs;
using Application.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class CreateDoorRequestValidatorTests
{
    private readonly CreateDoorRequestValidator _validator = new();

    private static CreateDoorRequest Valid() =>
        new(Name: "Front Door", Kind: DoorKind.Door, FromRoomId: 1, ToRoomId: 2, Wall: Wall.South,
            OffsetInches: 12, WidthInches: 36, HeightInches: 80, HingeSide: null, Swing: null);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void LeadsOutside_NullToRoom_IsAllowed() =>
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
    public void WidthInches_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { WidthInches = 0 })
            .ShouldHaveValidationErrorFor(x => x.WidthInches);

    [Fact]
    public void OffsetInches_WhenNegative_IsRejected() =>
        _validator.TestValidate(Valid() with { OffsetInches = -1 })
            .ShouldHaveValidationErrorFor(x => x.OffsetInches);
}
