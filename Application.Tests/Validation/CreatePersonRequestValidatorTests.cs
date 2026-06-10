using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class CreatePersonRequestValidatorTests
{
    private readonly CreatePersonRequestValidator _validator = new();

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(new CreatePersonRequest("Alex")).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Name_WhenEmpty_IsRejected() =>
        _validator.TestValidate(new CreatePersonRequest(""))
            .ShouldHaveValidationErrorFor(x => x.Name);

    [Fact]
    public void Name_LongerThan100_IsRejected() =>
        _validator.TestValidate(new CreatePersonRequest(new string('a', 101)))
            .ShouldHaveValidationErrorFor(x => x.Name);
}
