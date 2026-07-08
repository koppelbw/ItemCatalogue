using Application.DTOs;

namespace Application.StoragePorts;

public interface ICsvItemParser
{
    Task<CsvParseResult> ParseAsync(Stream csv, CancellationToken cancellationToken = default);
}
