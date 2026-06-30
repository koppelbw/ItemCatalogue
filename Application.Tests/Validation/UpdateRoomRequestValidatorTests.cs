using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class UpdateRoomRequestValidatorTests
{
    private readonly UpdateRoomRequestValidator _validator = new();

    private static UpdateRoomRequest Valid() =>
        new(Id: 1, Name: "Garage", Description: "Out back", FloorId: 3, RowVersion: [1, 2, 3]);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Id_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { Id = 0 })
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void FloorId_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { FloorId = 0 })
            .ShouldHaveValidationErrorFor(x => x.FloorId);

    [Fact]
    public void RowVersion_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { RowVersion = [] })
            .ShouldHaveValidationErrorFor(x => x.RowVersion);
}
