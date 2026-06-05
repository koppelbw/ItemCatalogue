namespace Domain.Pagination;

// Normalized, validated request for a single page of results. Constructed only through
// Create(...) so page/size bounds are enforced in one place, no matter where the raw
// values originate. This is what makes an unbounded "fetch the whole table" query
// impossible to express.
public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; }

    public int PageSize { get; }

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    // Clamp caller-supplied values into safe bounds: Page is 1-based (>= 1); PageSize is
    // 1..MaxPageSize, falling back to DefaultPageSize when missing or non-positive.
    public static PageRequest Create(int? page = null, int? pageSize = null)
    {
        var normalizedPage = page is > 0 ? page.Value : 1;
        var requestedSize = pageSize is > 0 ? pageSize.Value : DefaultPageSize;
        var normalizedSize = Math.Min(requestedSize, MaxPageSize);

        return new PageRequest(normalizedPage, normalizedSize);
    }

    // Rows to skip for this page in an OFFSET/FETCH query.
    public int Skip => (Page - 1) * PageSize;
}
