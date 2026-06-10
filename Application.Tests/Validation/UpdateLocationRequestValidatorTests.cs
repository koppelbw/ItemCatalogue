using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class UpdateLocationRequestValidatorTests
{
    private readonly UpdateLocationRequestValidator _validator = new();

    private static UpdateLocationRequest Valid() =>
        new(Id: 1, Name: "Top shelf", Description: null, RoomId: 5, RowVersion: [1]);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void RoomId_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { RoomId = -1 })
            .ShouldHaveValidationErrorFor(x => x.RoomId);

    [Fact]
    public void RowVersion_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { RowVersion = [] })
            .ShouldHaveValidationErrorFor(x => x.RowVersion);
}
