// src/EnterpriseApi.Application/Common/Models/PagedResult.cs
namespace JanaticsApi.Application.Common.Models;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    // Pagination metadata for Link header / HATEOAS
    public PaginationMetadata GetMetadata() => new(
        TotalCount, Page, PageSize, TotalPages, HasPreviousPage, HasNextPage);
}

public sealed record PaginationMetadata(
    int TotalCount,
    int CurrentPage,
    int PageSize,
    int TotalPages,
    bool HasPreviousPage,
    bool HasNextPage);