using Application.DTOs;
using Application.ServicePorts;
using ItemCatalogueFunctions;
using NSubstitute;

namespace ItemCatalogueFunctions.Tests;

// The functions are deliberately one-line shells over IImportJobService; these tests pin that
// delegation (message and cancellation token pass through untouched, nothing swallowed).
// The service's behavior itself is covered by Application.Tests/ImportJobServiceTests, and the
// end-to-end queue wiring by ItemCatalogueAPI.Tests/ImportApiTests.
public class ImportFunctionTests
{
    private readonly IImportJobService _service = Substitute.For<IImportJobService>();

    [Fact]
    public async Task ImportChunkFunction_DelegatesToProcessChunkAsync()
    {
        var message = new ImportChunkMessage(42, 1, 25, 25);
        using var cts = new CancellationTokenSource();

        await new ImportChunkFunction(_service).Run(message, cts.Token);

        await _service.Received(1).ProcessChunkAsync(message, cts.Token);
    }

    [Fact]
    public async Task ImportPoisonFunction_DelegatesToMarkChunkFailedAsync()
    {
        var message = new ImportChunkMessage(42, 1, 25, 25);
        using var cts = new CancellationTokenSource();

        await new ImportPoisonFunction(_service).Run(message, cts.Token);

        await _service.Received(1).MarkChunkFailedAsync(message, Arg.Is<string>(r => r.Contains("retries")), cts.Token);
    }

    [Fact]
    public async Task ImportChunkFunction_ServiceFailure_PropagatesSoTheHostRetries()
    {
        _service.ProcessChunkAsync(Arg.Any<ImportChunkMessage>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("db down")));

        // The exception must escape the function: that is what makes the host redeliver the
        // message and, eventually, poison it.
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => new ImportChunkFunction(_service).Run(new ImportChunkMessage(1, 0, 0, 25), CancellationToken.None));
    }
}
