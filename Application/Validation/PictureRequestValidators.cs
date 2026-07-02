using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

public sealed class UploadPictureRequestValidator : AbstractValidator<UploadPictureRequest>
{
    public UploadPictureRequestValidator()
    {
        RuleFor(x => x.OwnerId)
            .GreaterThan(0);

        RuleFor(x => x.ContentType)
            .Must(PictureValidationRules.AllowedContentTypes.Contains)
            .WithMessage($"ContentType must be one of: {string.Join(", ", PictureValidationRules.AllowedContentTypes)}.");

        RuleFor(x => x.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(PictureValidationRules.MaxSizeBytes)
            .WithMessage($"File size must not exceed {PictureValidationRules.MaxSizeBytes} bytes.");

        RuleFor(x => x.Caption)
            .MaximumLength(500);
    }
}

public sealed class UpdatePictureRequestValidator : AbstractValidator<UpdatePictureRequest>
{
    public UpdatePictureRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Caption)
            .MaximumLength(500);

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
