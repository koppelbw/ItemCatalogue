using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class CreateContainerRequestValidatorTests
{
    private readonly CreateContainerRequestValidator _validator = new();

    // Valid baseline: a top-level container owned by a room.
    private static CreateContainerRequest Valid() =>
        new(Name: "Dresser", Description: "Bedroom dresser", RoomId: 3, ParentContainerId: null);

    [Fact]
    public void TopLevelInRoom_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void NestedInContainer_HasNoErrors() =>
        _validator.TestValidate(Valid() with { RoomId = null, ParentContainerId = 2 })
            .ShouldNotHaveAnyValidationErrors();

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
    public void BothRoomAndParent_IsRejected() =>
        _validator.TestValidate(Valid() with { RoomId = 3, ParentContainerId = 2 })
            .ShouldHaveValidationErrorFor("Owner");

    [Fact]
    public void NeitherRoomNorParent_IsRejected() =>
        _validator.TestValidate(Valid() with { RoomId = null, ParentContainerId = null })
            .ShouldHaveValidationErrorFor("Owner");

    [Fact]
    public void RoomId_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { RoomId = 0 })
            .ShouldHaveValidationErrorFor(x => x.RoomId);
}
