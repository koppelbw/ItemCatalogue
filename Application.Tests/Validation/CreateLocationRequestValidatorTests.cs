using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class CreateLocationRequestValidatorTests
{
    private readonly CreateLocationRequestValidator _validator = new();

    private static CreateLocationRequest Valid() =>
        new(Name: "Top shelf", Description: null);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { Name = "" })
            .ShouldHaveValidationErrorFor(x => x.Name);
}
