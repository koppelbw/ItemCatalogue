using Application.DTOs;
using FluentValidation;

namespace Application.Validation;

// Input rules for container writes. Limits mirror the EF Core column mappings in
// ItemCatalogueDbContext (Name nvarchar(100), Description nvarchar(500)). A container is owned by
// exactly one parent: either a Room (top-level) or another Container (nested) — never both, never
// neither. This XOR mirrors the CK_Container_RoomXorParent check constraint in the database.
public sealed class CreateContainerRequestValidator : AbstractValidator<CreateContainerRequest>
{
    public CreateContainerRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .When(x => x.RoomId.HasValue);

        RuleFor(x => x.ParentContainerId)
            .GreaterThan(0)
            .When(x => x.ParentContainerId.HasValue);

        RuleFor(x => x)
            .Must(x => x.RoomId.HasValue ^ x.ParentContainerId.HasValue)
            .WithMessage("Specify exactly one of RoomId or ParentContainerId.")
            .WithName("Owner");
    }
}

public sealed class UpdateContainerRequestValidator : AbstractValidator<UpdateContainerRequest>
{
    public UpdateContainerRequestValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0);

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Description)
            .MaximumLength(500);

        RuleFor(x => x.RoomId)
            .GreaterThan(0)
            .When(x => x.RoomId.HasValue);

        RuleFor(x => x.ParentContainerId)
            .GreaterThan(0)
            .When(x => x.ParentContainerId.HasValue);

        RuleFor(x => x)
            .Must(x => x.RoomId.HasValue ^ x.ParentContainerId.HasValue)
            .WithMessage("Specify exactly one of RoomId or ParentContainerId.")
            .WithName("Owner");

        RuleFor(x => x.RowVersion)
            .NotEmpty();
    }
}
