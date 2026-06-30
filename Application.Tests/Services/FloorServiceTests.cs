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

public class FloorServiceTests
{
    private readonly IFloorRepository _repository = Substitute.For<IFloorRepository>();
    private readonly FloorService _service;

    public FloorServiceTests()
    {
        _service = new FloorService(
            _repository,
            new CreateFloorRequestValidator(),
            new UpdateFloorRequestValidator(),
            NullLogger<FloorService>.Instance);
    }

    private static Floor Existing(int id = 1) =>
        new() { Id = id, Name = "First Floor", LocationId = 3, LevelIndex = 0, RowVersion = [1, 2, 3] };

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Floor?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(5));
        ex.Message.ShouldBe("Floor with id 5 not found.");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_InsertsAndReturnsResponse()
    {
        _repository.When(r => r.InsertAsync(Arg.Any<Floor>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Floor>().Id = 10);

        var response = await _service.CreateAsync(new CreateFloorRequest("Basement", 3, -1, -96m, 84m));

        response.Id.ShouldBe(10);
        response.Name.ShouldBe("Basement");
        response.LevelIndex.ShouldBe(-1);
        await _repository.Received(1).InsertAsync(Arg.Any<Floor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ThrowsAndDoesNotInsert()
    {
        await Should.ThrowAsync<ValidationException>(() => _service.CreateAsync(new CreateFloorRequest("", 3, 0, null, null)));

        await _repository.DidNotReceive().InsertAsync(Arg.Any<Floor>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_AppliesChangesAndUpdates()
    {
        var existing = Existing();
        _repository.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var response = await _service.UpdateAsync(new UpdateFloorRequest(1, "Attic", 3, 2, null, null, [9]));

        response.Name.ShouldBe("Attic");
        existing.Name.ShouldBe("Attic");
        existing.LevelIndex.ShouldBe(2);
        existing.RowVersion.ShouldBe([9]);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((Floor?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => _service.UpdateAsync(new UpdateFloorRequest(2, "Attic", 3, 2, null, null, [9])));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Floor>(), Arg.Any<CancellationToken>());
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
        _repository.DeleteAsync(4, Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new EntityInUseException("Floor is in use."));

        await Should.ThrowAsync<EntityInUseException>(() => _service.DeleteAsync(4));
    }
}
