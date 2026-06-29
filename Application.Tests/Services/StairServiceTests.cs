using Application.DTOs;
using Application.Services;
using Application.Validation;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.RepositoryPorts;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Services;

public class StairServiceTests
{
    private readonly IStairRepository _repository = Substitute.For<IStairRepository>();
    private readonly StairService _service;

    public StairServiceTests()
    {
        _service = new StairService(
            _repository,
            new CreateStairRequestValidator(),
            new UpdateStairRequestValidator(),
            NullLogger<StairService>.Instance);
    }

    private static Stair Existing(int id = 1) => new()
    {
        Id = id,
        Name = "Basement Stairs",
        Shape = StairShape.Straight,
        FromRoomId = 1,
        ToRoomId = 2,
        RowVersion = [1, 2, 3],
    };

    private static CreateStairRequest ValidCreate() =>
        new("Basement Stairs", FromRoomId: 1, ToRoomId: 2, StairShape.Straight);

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Stair?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(5));
        ex.Message.ShouldBe("Stair with id 5 not found.");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_InsertsAndReturnsResponse()
    {
        _repository.When(r => r.InsertAsync(Arg.Any<Stair>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Stair>().Id = 10);

        var response = await _service.CreateAsync(ValidCreate());

        response.Id.ShouldBe(10);
        response.Shape.ShouldBe(StairShape.Straight);
        response.ToRoomId.ShouldBe(2);
        await _repository.Received(1).InsertAsync(Arg.Any<Stair>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenConnectingRoomToItself_ThrowsAndDoesNotInsert()
    {
        await Should.ThrowAsync<ValidationException>(
            () => _service.CreateAsync(ValidCreate() with { ToRoomId = 1 }));

        await _repository.DidNotReceive().InsertAsync(Arg.Any<Stair>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_AppliesChangesAndUpdates()
    {
        var existing = Existing();
        _repository.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var response = await _service.UpdateAsync(
            new UpdateStairRequest(1, "Spiral", FromRoomId: 1, ToRoomId: 2, StairShape.Spiral, [9], StepCount: 16));

        response.Name.ShouldBe("Spiral");
        existing.Shape.ShouldBe(StairShape.Spiral);
        existing.StepCount.ShouldBe(16);
        existing.RowVersion.ShouldBe([9]);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((Stair?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => _service.UpdateAsync(new UpdateStairRequest(2, null, 1, null, StairShape.Straight, [9])));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Stair>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsRowsAffected()
    {
        _repository.DeleteAsync(4, Arg.Any<CancellationToken>()).Returns(1);

        (await _service.DeleteAsync(4)).ShouldBe(1);
        await _repository.Received(1).DeleteAsync(4, Arg.Any<CancellationToken>());
    }
}
