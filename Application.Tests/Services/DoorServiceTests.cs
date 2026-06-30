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

public class DoorServiceTests
{
    private readonly IDoorRepository _repository = Substitute.For<IDoorRepository>();
    private readonly DoorService _service;

    public DoorServiceTests()
    {
        _service = new DoorService(
            _repository,
            new CreateDoorRequestValidator(),
            new UpdateDoorRequestValidator(),
            NullLogger<DoorService>.Instance);
    }

    private static Door Existing(int id = 1) => new()
    {
        Id = id,
        Name = "Front Door",
        Kind = DoorKind.Door,
        FromRoomId = 1,
        Wall = Wall.South,
        OffsetInches = 12,
        WidthInches = 36,
        HeightInches = 80,
        RowVersion = [1, 2, 3],
    };

    private static CreateDoorRequest ValidCreate() =>
        new("Front Door", DoorKind.Door, FromRoomId: 1, ToRoomId: null, Wall.South, OffsetInches: 12, WidthInches: 36, HeightInches: 80, HingeSide: null, Swing: null);

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Door?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(5));
        ex.Message.ShouldBe("Door with id 5 not found.");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_InsertsAndReturnsResponse()
    {
        _repository.When(r => r.InsertAsync(Arg.Any<Door>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Door>().Id = 10);

        var response = await _service.CreateAsync(ValidCreate());

        response.Id.ShouldBe(10);
        response.Kind.ShouldBe(DoorKind.Door);
        response.ToRoomId.ShouldBeNull();
        await _repository.Received(1).InsertAsync(Arg.Any<Door>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WhenConnectingRoomToItself_ThrowsAndDoesNotInsert()
    {
        await Should.ThrowAsync<ValidationException>(
            () => _service.CreateAsync(ValidCreate() with { ToRoomId = 1 }));

        await _repository.DidNotReceive().InsertAsync(Arg.Any<Door>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_AppliesChangesAndUpdates()
    {
        var existing = Existing();
        _repository.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var response = await _service.UpdateAsync(
            new UpdateDoorRequest(1, "Side Door", DoorKind.SlidingDoor, FromRoomId: 1, ToRoomId: 2, Wall.East, 6, 32, 80, null, null, [9]));

        response.Name.ShouldBe("Side Door");
        existing.Kind.ShouldBe(DoorKind.SlidingDoor);
        existing.ToRoomId.ShouldBe(2);
        existing.RowVersion.ShouldBe([9]);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((Door?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => _service.UpdateAsync(new UpdateDoorRequest(2, null, DoorKind.Door, 1, null, Wall.North, 0, 30, 80, null, null, [9])));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Door>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ReturnsRowsAffected()
    {
        _repository.DeleteAsync(4, Arg.Any<CancellationToken>()).Returns(1);

        (await _service.DeleteAsync(4)).ShouldBe(1);
        await _repository.Received(1).DeleteAsync(4, Arg.Any<CancellationToken>());
    }
}
