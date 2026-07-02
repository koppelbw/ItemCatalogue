using Application.DTOs;
using Application.Validation;
using Domain.Enums;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class UploadPictureRequestValidatorTests
{
    private readonly UploadPictureRequestValidator _validator = new();

    private static UploadPictureRequest Valid() =>
        new(PictureOwnerType.Item, 1, Stream.Null, "image/jpeg", "photo.jpg", 1024, "A caption", false);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void OwnerId_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { OwnerId = 0 })
            .ShouldHaveValidationErrorFor(x => x.OwnerId);

    [Fact]
    public void ContentType_WhenNotAllowed_IsRejected() =>
        _validator.TestValidate(Valid() with { ContentType = "application/pdf" })
            .ShouldHaveValidationErrorFor(x => x.ContentType);

    [Theory]
    [InlineData("image/jpeg")]
    [InlineData("image/png")]
    [InlineData("image/webp")]
    [InlineData("image/gif")]
    public void ContentType_WhenAllowed_HasNoError(string contentType) =>
        _validator.TestValidate(Valid() with { ContentType = contentType })
            .ShouldNotHaveValidationErrorFor(x => x.ContentType);

    [Fact]
    public void SizeBytes_WhenZero_IsRejected() =>
        _validator.TestValidate(Valid() with { SizeBytes = 0 })
            .ShouldHaveValidationErrorFor(x => x.SizeBytes);

    [Fact]
    public void SizeBytes_WhenExceedsMax_IsRejected() =>
        _validator.TestValidate(Valid() with { SizeBytes = PictureValidationRules.MaxSizeBytes + 1 })
            .ShouldHaveValidationErrorFor(x => x.SizeBytes);

    [Fact]
    public void Caption_LongerThan500_IsRejected() =>
        _validator.TestValidate(Valid() with { Caption = new string('a', 501) })
            .ShouldHaveValidationErrorFor(x => x.Caption);
}

public class UpdatePictureRequestValidatorTests
{
    private readonly UpdatePictureRequestValidator _validator = new();

    private static UpdatePictureRequest Valid() =>
        new(Id: 1, Caption: "Caption", IsPrimary: true, SortOrder: 0, RowVersion: [1, 2, 3]);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Id_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { Id = 0 })
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void Caption_LongerThan500_IsRejected() =>
        _validator.TestValidate(Valid() with { Caption = new string('a', 501) })
            .ShouldHaveValidationErrorFor(x => x.Caption);

    [Fact]
    public void SortOrder_WhenNegative_IsRejected() =>
        _validator.TestValidate(Valid() with { SortOrder = -1 })
            .ShouldHaveValidationErrorFor(x => x.SortOrder);

    [Fact]
    public void RowVersion_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { RowVersion = [] })
            .ShouldHaveValidationErrorFor(x => x.RowVersion);
}
