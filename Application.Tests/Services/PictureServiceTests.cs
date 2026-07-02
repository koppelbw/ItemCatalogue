using Application.DTOs;
using Application.Services;
using Application.StoragePorts;
using Application.Validation;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Services;

public class PictureServiceTests
{
    private readonly IPictureRepository _repository = Substitute.For<IPictureRepository>();
    private readonly IImageStorage _storage = Substitute.For<IImageStorage>();
    private readonly PictureService _service;

    public PictureServiceTests()
    {
        _service = new PictureService(
            _repository,
            _storage,
            new UploadPictureRequestValidator(),
            new UpdatePictureRequestValidator(),
            NullLogger<PictureService>.Instance);
    }

    // Minimal valid signature bytes for each allowed content type, so the service's magic-byte
    // sniff (PictureService.MatchesDeclaredContentTypeAsync) accepts the stream.
    private static Stream JpegBytes() => new MemoryStream([0xFF, 0xD8, 0xFF, 0x00, 0x00, 0x00]);

    private static UploadPictureRequest ValidUpload(PictureOwnerType ownerType = PictureOwnerType.Item, int ownerId = 1, bool isPrimary = false) =>
        new(ownerType, ownerId, JpegBytes(), "image/jpeg", "photo.jpg", 1024, "A caption", isPrimary);

    private static Picture Existing(int id = 1) => new()
    {
        Id = id,
        BlobName = "item/1/existing.jpg",
        ContentType = "image/jpeg",
        SizeBytes = 1024,
        ItemId = 1,
        RowVersion = [1, 2, 3],
    };

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Picture?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(5));
        ex.Message.ShouldBe("Picture with id 5 not found.");
    }

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsResponseWithSasUrl()
    {
        var picture = Existing();
        var sasUrl = new Uri("https://blob.example/item/1/existing.jpg?sas=token");
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(picture);
        _storage.GetReadUrl(picture.BlobName).Returns(sasUrl);

        var response = await _service.GetByIdAsync(1);

        response.Url.ShouldBe(sasUrl);
        response.OwnerType.ShouldBe(PictureOwnerType.Item);
        response.OwnerId.ShouldBe(1);
    }

    [Fact]
    public async Task UploadAsync_ValidImage_UploadsThenInsertsAndReturnsResponseWithUrl()
    {
        _storage.UploadAsync(Arg.Any<NewImage>(), Arg.Any<CancellationToken>())
            .Returns(new StoredImage("item/1/new-guid.jpg", 2048));
        var sasUrl = new Uri("https://blob.example/item/1/new-guid.jpg?sas=token");
        _storage.GetReadUrl("item/1/new-guid.jpg").Returns(sasUrl);
        _repository.When(r => r.InsertAsync(Arg.Any<Picture>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Picture>().Id = 42);

        var response = await _service.UploadAsync(ValidUpload());

        response.Id.ShouldBe(42);
        response.Url.ShouldBe(sasUrl);
        response.SizeBytes.ShouldBe(2048);
        await _storage.Received(1).UploadAsync(
            Arg.Is<NewImage>(u => u.OwnerType == PictureOwnerType.Item && u.OwnerId == 1 && u.FileExtension == ".jpg"),
            Arg.Any<CancellationToken>());
        await _repository.Received(1).InsertAsync(Arg.Any<Picture>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_ContentDoesNotMatchDeclaredType_ThrowsAndDoesNotUpload()
    {
        // Declares image/jpeg but the bytes don't carry the JPEG signature.
        var request = ValidUpload() with { Content = new MemoryStream([0x00, 0x01, 0x02, 0x03]) };

        await Should.ThrowAsync<ValidationException>(() => _service.UploadAsync(request));

        await _storage.DidNotReceive().UploadAsync(Arg.Any<NewImage>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().InsertAsync(Arg.Any<Picture>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WhenIsPrimary_ClearsSiblingsPrimary()
    {
        _storage.UploadAsync(Arg.Any<NewImage>(), Arg.Any<CancellationToken>())
            .Returns(new StoredImage("item/1/new-guid.jpg", 2048));
        _storage.GetReadUrl(Arg.Any<string>()).Returns(new Uri("https://blob.example/x"));
        _repository.When(r => r.InsertAsync(Arg.Any<Picture>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Picture>().Id = 42);

        await _service.UploadAsync(ValidUpload(isPrimary: true));

        await _repository.Received(1).ClearPrimaryForOwnerAsync(
            PictureOwnerType.Item, 1, 42, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UploadAsync_WhenNotPrimary_DoesNotClearSiblingsPrimary()
    {
        _storage.UploadAsync(Arg.Any<NewImage>(), Arg.Any<CancellationToken>())
            .Returns(new StoredImage("item/1/new-guid.jpg", 2048));
        _storage.GetReadUrl(Arg.Any<string>()).Returns(new Uri("https://blob.example/x"));

        await _service.UploadAsync(ValidUpload(isPrimary: false));

        await _repository.DidNotReceive().ClearPrimaryForOwnerAsync(
            Arg.Any<PictureOwnerType>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_AppliesChangesAndReturnsResponse()
    {
        var existing = Existing();
        _repository.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _storage.GetReadUrl(existing.BlobName).Returns(new Uri("https://blob.example/x"));

        var response = await _service.UpdateAsync(new UpdatePictureRequest(1, "New caption", false, 2, [9]));

        response.Caption.ShouldBe("New caption");
        response.SortOrder.ShouldBe(2);
        existing.RowVersion.ShouldBe([9]);
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateAsync(2, Arg.Any<CancellationToken>()).Returns((Picture?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => _service.UpdateAsync(new UpdatePictureRequest(2, null, false, 0, [1])));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Picture>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_DeletesRowThenBlob()
    {
        var existing = Existing();
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(existing);
        _repository.DeleteAsync(1, Arg.Any<CancellationToken>()).Returns(1);

        var rowsAffected = await _service.DeleteAsync(1);

        rowsAffected.ShouldBe(1);
        Received.InOrder(() =>
        {
            _repository.DeleteAsync(1, Arg.Any<CancellationToken>());
            _storage.DeleteAsync(existing.BlobName, Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Picture?)null);

        await Should.ThrowAsync<NotFoundException>(() => _service.DeleteAsync(5));

        await _storage.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
