namespace BlazorUI.Contracts
{
    public sealed record LocationResponse(
        int Id,
        string Name,
        string? Description,
        IReadOnlyList<FloorResponse> Floors,
        byte[] RowVersion);

    public sealed record FloorResponse(
        int Id,
        string Name,
        int LocationId,
        int LevelIndex,
        decimal? ElevationInches,
        decimal? CeilingHeightInches,
        byte[] RowVersion);
}
