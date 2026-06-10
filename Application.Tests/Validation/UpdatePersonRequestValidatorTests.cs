using Application.DTOs;
using Application.Validation;
using FluentValidation.TestHelper;

namespace Application.Tests.Validation;

public class UpdatePersonRequestValidatorTests
{
    private readonly UpdatePersonRequestValidator _validator = new();

    private static UpdatePersonRequest Valid() => new(Id: 1, Name: "Alex", RowVersion: [1]);

    [Fact]
    public void ValidRequest_HasNoErrors() =>
        _validator.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Id_WhenNotPositive_IsRejected() =>
        _validator.TestValidate(Valid() with { Id = 0 })
            .ShouldHaveValidationErrorFor(x => x.Id);

    [Fact]
    public void RowVersion_WhenEmpty_IsRejected() =>
        _validator.TestValidate(Valid() with { RowVersion = [] })
            .ShouldHaveValidationErrorFor(x => x.RowVersion);
}
