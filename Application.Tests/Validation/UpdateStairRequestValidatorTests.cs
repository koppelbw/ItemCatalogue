using Application.DTOs;
using Application.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class UpdateStairRequestValidatorTests
{
    private readonly UpdateStairRequestValidator _validator = new();

    private static UpdateStairRequest Valid() =>
        new(Id: 1, Name: "Basement Stairs", FromRoomId: 1, ToRoomId: 2, Shape: StairShape.Straight, RowVersion: [1, 2, 3]);

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
