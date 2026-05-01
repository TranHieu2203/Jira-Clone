namespace BB.Common;

public sealed record PagedList<T>(
    IReadOnlyList<T> Items,
    long TotalCount,
    int PageIndex,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageIndex > 1;
    public bool HasNext => PageIndex < TotalPages;

    public static PagedList<T> Empty(int pageIndex = 1, int pageSize = 20) =>
        new(Array.Empty<T>(), 0, pageIndex, pageSize);
}

public sealed record PagedRequest(int PageIndex = 1, int PageSize = 20, string? Sort = null, string? Search = null);
