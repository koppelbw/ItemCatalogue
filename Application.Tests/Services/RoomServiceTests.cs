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

public class RoomServiceTests
{
    private readonly IRoomRepository _repository = Substitute.For<IRoomRepository>();
    private readonly RoomService _service;

    public RoomServiceTests()
    {
        _service = new RoomService(
            _repository,
            new CreateRoomRequestValidator(),
            new UpdateRoomRequestValidator(),
            NullLogger<RoomService>.Instance);
    }

    private static Room Existing(int id = 1) =>
        new() { Id = id, Name = "Garage", RowVersion = [1, 2, 3] };

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Room?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(5));
        ex.Message.ShouldBe("Room with id 5 not found.");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_InsertsAndReturnsResponse()
    {
        _repository.When(r => r.InsertAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Room>().Id = 10);

        var response = await _service.CreateAsync(new CreateRoomRequest("Garage", "Out back"));

        response.Id.ShouldBe(10);
        response.Name.ShouldBe("Garage");
        await _repository.Received(1).InsertAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ThrowsAndDoesNotInsert()
    {
        await Should.ThrowAsync<ValidationException>(() => _service.CreateAsync(new CreateRoomRequest("", null)));

        await _repository.DidNotReceive().InsertAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_AppliesChangesAndUpdates()
    {
        var existing = Existing();
        _repository.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var response = await _service.UpdateAsync(new UpdateRoomRequest(1, "Shed", "Renamed", [9]));

        response.Name.ShouldBe("Shed");
        existing.Name.ShouldBe("Shed");
        existing.RowVersion.ShouldBe([9]);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((Room?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => _service.UpdateAsync(new UpdateRoomRequest(2, "Shed", null, [9])));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Room>(), Arg.Any<CancellationToken>());
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
            .Returns<int>(_ => throw new EntityInUseException("Room is in use."));

        await Should.ThrowAsync<EntityInUseException>(() => _service.DeleteAsync(4));
    }
}
