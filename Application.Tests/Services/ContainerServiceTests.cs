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

public class ContainerServiceTests
{
    private readonly IContainerRepository _repository = Substitute.For<IContainerRepository>();
    private readonly ContainerService _service;

    public ContainerServiceTests()
    {
        _service = new ContainerService(
            _repository,
            new CreateContainerRequestValidator(),
            new UpdateContainerRequestValidator(),
            NullLogger<ContainerService>.Instance);
    }

    private static Container Existing(int id = 1) =>
        new() { Id = id, Name = "Dresser", RoomId = 3, RowVersion = [1, 2, 3] };

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Container?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(5));
        ex.Message.ShouldBe("Container with id 5 not found.");
    }

    [Fact]
    public async Task CreateAsync_TopLevelInRoom_InsertsAndReturnsResponse()
    {
        _repository.When(r => r.InsertAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Container>().Id = 10);

        var response = await _service.CreateAsync(new CreateContainerRequest("Dresser", "Bedroom dresser", RoomId: 3, ParentContainerId: null));

        response.Id.ShouldBe(10);
        response.Name.ShouldBe("Dresser");
        response.RoomId.ShouldBe(3);
        await _repository.Received(1).InsertAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_NestedInContainer_InsertsAndReturnsResponse()
    {
        _repository.When(r => r.InsertAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Container>().Id = 11);

        var response = await _service.CreateAsync(new CreateContainerRequest("Box", null, RoomId: null, ParentContainerId: 2));

        response.Id.ShouldBe(11);
        response.ParentContainerId.ShouldBe(2);
    }

    [Fact]
    public async Task CreateAsync_WithBothRoomAndParent_ThrowsAndDoesNotInsert()
    {
        await Should.ThrowAsync<ValidationException>(
            () => _service.CreateAsync(new CreateContainerRequest("Dresser", null, RoomId: 3, ParentContainerId: 2)));

        await _repository.DidNotReceive().InsertAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithNeitherRoomNorParent_ThrowsAndDoesNotInsert()
    {
        await Should.ThrowAsync<ValidationException>(
            () => _service.CreateAsync(new CreateContainerRequest("Dresser", null, RoomId: null, ParentContainerId: null)));

        await _repository.DidNotReceive().InsertAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_AppliesChangesAndUpdates()
    {
        var existing = Existing();
        _repository.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var response = await _service.UpdateAsync(new UpdateContainerRequest(1, "Wardrobe", "Renamed", RoomId: 7, ParentContainerId: null, [9]));

        response.Name.ShouldBe("Wardrobe");
        existing.Name.ShouldBe("Wardrobe");
        existing.RoomId.ShouldBe(7);
        existing.RowVersion.ShouldBe([9]);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((Container?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => _service.UpdateAsync(new UpdateContainerRequest(2, "Wardrobe", null, RoomId: 7, ParentContainerId: null, [9])));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Container>(), Arg.Any<CancellationToken>());
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
        // The repository translates an FK-restrict violation (a container that still has children)
        // into EntityInUseException; the service must let it bubble so the API maps it to 409.
        _repository.DeleteAsync(4, Arg.Any<CancellationToken>())
            .Returns<int>(_ => throw new EntityInUseException("Container is in use."));

        await Should.ThrowAsync<EntityInUseException>(() => _service.DeleteAsync(4));
    }
}
