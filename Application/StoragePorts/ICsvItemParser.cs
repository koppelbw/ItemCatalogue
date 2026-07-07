using Application.DTOs;

namespace Application.StoragePorts;

// Format port for the CSV upload (implemented by Infrastructure/Storage/CsvItemParser).
// Pure parsing: cell text -> typed CsvItemRow values, with per-row errors for cells that don't
// parse (bad date, non-numeric price, unknown enum name). No database access and no business
// validation — reference resolution and FluentValidation happen later in the import service,
// keeping this port independently testable and the core format-agnostic.
public interface ICsvItemParser
{
    Task<CsvParseResult> ParseAsync(Stream csv, CancellationToken cancellationToken = default);
}
