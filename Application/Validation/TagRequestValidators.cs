using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

public sealed class CreateTagRequestValidator : AbstractValidator<CreateTagRequest>
{
    public CreateTagRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);
    }
}

public sealed class UpdateTagRequestValidator : AbstractValidator<UpdateTagRequest>
{
    public UpdateTagRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}

public sealed class SetItemTagsRequestValidator : AbstractValidator<SetItemTagsRequest>
{
    public SetItemTagsRequestValidator()
    {
        RuleFor(x => x.TagIds)
            .NotNull();

        RuleForEach(x => x.TagIds)
            .GreaterThan(0);
    }
}
