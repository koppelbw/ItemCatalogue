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

public class LocationServiceTests
{
    private readonly ILocationRepository _repository = Substitute.For<ILocationRepository>();
    private readonly LocationService _service;

    public LocationServiceTests()
    {
        _service = new LocationService(
            _repository,
            new CreateLocationRequestValidator(),
            new UpdateLocationRequestValidator(),
            NullLogger<LocationService>.Instance);
    }

    private static Location Existing(int id = 1) =>
        new() { Id = id, Name = "Top shelf", RowVersion = [1, 2, 3] };

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Location?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(5));
        ex.Message.ShouldBe("Location with id 5 not found.");
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_InsertsAndReturnsResponse()
    {
        _repository.When(r => r.InsertAsync(Arg.Any<Location>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Location>().Id = 10);

        var response = await _service.CreateAsync(new CreateLocationRequest("Top shelf", "By the door"));

        response.Id.ShouldBe(10);
        response.Name.ShouldBe("Top shelf");
        await _repository.Received(1).InsertAsync(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ThrowsAndDoesNotInsert()
    {
        await Should.ThrowAsync<ValidationException>(
            () => _service.CreateAsync(new CreateLocationRequest("", null)));

        await _repository.DidNotReceive().InsertAsync(Arg.Any<Location>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_AppliesChangesAndUpdates()
    {
        var existing = Existing();
        _repository.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var response = await _service.UpdateAsync(new UpdateLocationRequest(1, "Bottom shelf", "Renamed", [9]));

        response.Name.ShouldBe("Bottom shelf");
        existing.Name.ShouldBe("Bottom shelf");
        existing.RowVersion.ShouldBe([9]);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((Location?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => _service.UpdateAsync(new UpdateLocationRequest(2, "Bottom shelf", null, [9])));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Location>(), Arg.Any<CancellationToken>());
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
            .Returns<int>(_ => throw new EntityInUseException("Location is in use."));

        await Should.ThrowAsync<EntityInUseException>(() => _service.DeleteAsync(4));
    }
}
