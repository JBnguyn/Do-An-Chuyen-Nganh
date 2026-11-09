using Microsoft.EntityFrameworkCore;

namespace AutoPartsManagement.Services.Common;

/// <summary>
/// Thông tin phân trang (không phụ thuộc kiểu dữ liệu) - dùng cho partial view _Pagination.
/// </summary>
public interface IPagedResult
{
    int PageIndex { get; }
    int PageSize { get; }
    int TotalCount { get; }
    int TotalPages { get; }
}

/// <summary>
/// Kết quả truy vấn có phân trang.
/// </summary>
public class PagedResult<T> : IPagedResult
{
    public PagedResult(IReadOnlyList<T> items, int pageIndex, int pageSize, int totalCount)
    {
        Items = items;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int TotalCount { get; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPrevious => PageIndex > 1;
    public bool HasNext => PageIndex < TotalPages;
}

public static class QueryableExtensions
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;

    /// <summary>
    /// Đếm tổng số bản ghi và lấy ra đúng trang cần hiển thị.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagedResultAsync<T>(this IQueryable<T> query, int page, int pageSize)
    {
        if (pageSize < 1) pageSize = DefaultPageSize;
        if (pageSize > MaxPageSize) pageSize = MaxPageSize;

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        if (page < 1) page = 1;
        if (totalPages > 0 && page > totalPages) page = totalPages;

        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<T>(items, page, pageSize, totalCount);
    }
}
