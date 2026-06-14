using Application.DTOs;
using Application.Services;
using Application.Validation;
using Domain.Entities;
using Domain.Exceptions;
using Domain.RepositoryPorts;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Services;

public class TagServiceTests
{
    private readonly ITagRepository _repository = Substitute.For<ITagRepository>();
    private readonly TagService _service;

    public TagServiceTests()
    {
        _service = new TagService(
            _repository,
            new CreateTagRequestValidator(),
            new UpdateTagRequestValidator(),
            NullLogger<TagService>.Instance);
    }

    private static Tag Existing(int id = 1) =>
        new() { Id = id, Name = "Fragile", RowVersion = [1, 2, 3] };

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(5));
        ex.Message.ShouldBe("Tag with id 5 not found.");
    }

    [Fact]
    public async Task CreateAsync_InsertsAndReturnsResponse()
    {
        _repository.When(r => r.InsertAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Tag>().Id = 10);

        var response = await _service.CreateAsync(new CreateTagRequest("Fragile", "Handle with care"));

        response.Id.ShouldBe(10);
        response.Name.ShouldBe("Fragile");
        await _repository.Received(1).InsertAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithEmptyName_ThrowsAndDoesNotInsert()
    {
        await Should.ThrowAsync<ValidationException>(() => _service.CreateAsync(new CreateTagRequest("", null)));

        await _repository.DidNotReceive().InsertAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_AppliesChangesAndUpdates()
    {
        var existing = Existing();
        _repository.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var response = await _service.UpdateAsync(new UpdateTagRequest(1, "Delicate", "Renamed", [9]));

        response.Name.ShouldBe("Delicate");
        existing.Name.ShouldBe("Delicate");
        existing.RowVersion.ShouldBe([9]);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((Tag?)null);

        await Should.ThrowAsync<NotFoundException>(() => _service.UpdateAsync(new UpdateTagRequest(2, "X", null, [9])));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsRowsAffected()
    {
        _repository.DeleteAsync(4, Arg.Any<CancellationToken>()).Returns(1);

        (await _service.DeleteAsync(4)).ShouldBe(1);
        await _repository.Received(1).DeleteAsync(4, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenRepositoryReportsDuplicate_PropagatesException()
    {
        // The repository translates a unique-name violation into DuplicateException; the service must
        // let it bubble so the API maps it to 409.
        _repository.When(r => r.InsertAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new DuplicateException("A Tag with the same unique value already exists."));

        await Should.ThrowAsync<DuplicateException>(() => _service.CreateAsync(new CreateTagRequest("Fragile", null)));
    }
}
