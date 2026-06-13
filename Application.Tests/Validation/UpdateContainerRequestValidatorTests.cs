using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class UpdateContainerRequestValidatorTests
{
    private readonly UpdateContainerRequestValidator _validator = new();

    private static UpdateContainerRequest Valid() =>
        new(Id: 1, Name: "Dresser", Description: "Bedroom dresser", RoomId: 3, ParentContainerId: null, RowVersion: [1, 2, 3]);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Id_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { Id = 0 })
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void BothRoomAndParent_IsRejected() =>
        _validator.TestValidate(Valid() with { RoomId = 3, ParentContainerId = 2 })
            .ShouldHaveValidationErrorFor("Owner");

    [Fact]
    public void NeitherRoomNorParent_IsRejected() =>
        _validator.TestValidate(Valid() with { RoomId = null, ParentContainerId = null })
            .ShouldHaveValidationErrorFor("Owner");

    [Fact]
    public void RowVersion_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { RowVersion = [] })
            .ShouldHaveValidationErrorFor(x => x.RowVersion);
}
