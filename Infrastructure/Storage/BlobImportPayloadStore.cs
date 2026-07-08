using Application.DTOs;
using Application.StoragePorts;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infrastructure.Storage;

// Claim-check blob adapter for IImportPayloadStore. The blob name is derived from the job id
// (import/{jobId}/payload.json), so nothing about payload location needs to be persisted or
// carried in queue messages. Builds its own client from ImportStorageOptions rather than reusing
// the image BlobServiceClient, so import storage can point at a different account via config.
public sealed class BlobImportPayloadStore(IOptions<ImportStorageOptions> options) : IImportPayloadStore
{
    private readonly BlobContainerClient _container = new(options.Value.ConnectionString, options.Value.PayloadContainerName);

    public async Task WriteAsync(int jobId, IReadOnlyList<ImportPayloadRow> rows, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(PayloadBlobName(jobId));
        await blob.UploadAsync(
            BinaryData.FromString(JsonSerializer.Serialize(rows)),
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = "application/json" } },
            cancellationToken);
    }

    public async Task<IReadOnlyList<ImportPayloadRow>> ReadChunkAsync(int jobId, int startRow, int count, CancellationToken cancellationToken = default)
    {
        var blob = _container.GetBlobClient(PayloadBlobName(jobId));

        // The whole payload is downloaded and sliced in memory: at MaxRows (~1000) it is a few
        // hundred KB, not worth a range-read format. A missing blob throws — the queue retry /
        // poison path owns that failure, not this adapter.
        var content = await blob.DownloadContentAsync(cancellationToken);
        var rows = content.Value.Content.ToObjectFromJson<List<ImportPayloadRow>>() ?? [];
        return rows.Skip(startRow).Take(count).ToList();
    }

    private static string PayloadBlobName(int jobId) => $"import/{jobId}/payload.json";
}
