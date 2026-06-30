using Application.DTOs;
using Application.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class UpdateDoorRequestValidatorTests
{
    private readonly UpdateDoorRequestValidator _validator = new();

    private static UpdateDoorRequest Valid() =>
        new(Id: 1, Name: "Front Door", Kind: DoorKind.Door, FromRoomId: 1, ToRoomId: 2, Wall: Wall.South,
            OffsetInches: 12, WidthInches: 36, HeightInches: 80, HingeSide: null, Swing: null, RowVersion: [1, 2, 3]);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Id_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { Id = 0 })
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void ToRoomId_EqualToFromRoomId_IsRejected() =>
        _validator.TestValidate(Valid() with { FromRoomId = 2, ToRoomId = 2 })
            .ShouldHaveValidationErrorFor(x => x.ToRoomId);

    [Fact]
    public void RowVersion_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { RowVersion = [] })
            .ShouldHaveValidationErrorFor(x => x.RowVersion);
}
