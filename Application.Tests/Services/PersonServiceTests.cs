using Application.DTOs;
using Application.Services;
using Application.Validation;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Services;

public class PersonServiceTests
{
    private readonly IPersonRepository _repository = Substitute.For<IPersonRepository>();
    private readonly PersonService _service;

    public PersonServiceTests()
    {
        _service = new PersonService(
            _repository,
            new CreatePersonRequestValidator(),
            new UpdatePersonRequestValidator(),
            NullLogger<PersonService>.Instance);
    }

    private static Person Existing(int id = 1) =>
        new() { Id = id, Name = "Alex", RowVersion = [1, 2, 3] };

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Person?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(5));
        ex.Message.ShouldBe("Person with id 5 not found.");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_InsertsAndReturnsResponse()
    {
        _repository.When(r => r.InsertAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Person>().Id = 10);

        var response = await _service.CreateAsync(new CreatePersonRequest("Alex"));

        response.Id.ShouldBe(10);
        response.Name.ShouldBe("Alex");
        await _repository.Received(1).InsertAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ThrowsAndDoesNotInsert()
    {
        await Should.ThrowAsync<ValidationException>(() => _service.CreateAsync(new CreatePersonRequest("")));

        await _repository.DidNotReceive().InsertAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_AppliesChangesAndUpdates()
    {
        var existing = Existing();
        _repository.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var response = await _service.UpdateAsync(new UpdatePersonRequest(1, "Jordan", [9]));

        response.Name.ShouldBe("Jordan");
        existing.Name.ShouldBe("Jordan");
        existing.RowVersion.ShouldBe([9]);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((Person?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => _service.UpdateAsync(new UpdatePersonRequest(2, "Jordan", [9])));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Person>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsRowsAffected()
    {
        _repository.DeleteAsync(4, Arg.Any<CancellationToken>()).Returns(1);

        (await _service.DeleteAsync(4)).ShouldBe(1);
        await _repository.Received(1).DeleteAsync(4, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_WhenRepositoryReportsEntityInUse_PropagatesException()
    {
        // The repository translates an FK-restrict violation into EntityInUseException; the
        // service must let it bubble so the API can map it to 409 Conflict.
        _repository.DeleteAsync(4, Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new EntityInUseException("Person is in use."));

        await Should.ThrowAsync<EntityInUseException>(() => _service.DeleteAsync(4));
    }
}
