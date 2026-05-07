// All handlers must accept and forward CancellationToken
using JanaticsApi.Application.Common.Models;

public sealed class GetUsersPagedQueryHandler
    : IRequestHandler<GetUsersPagedQuery, Result<PagedResult<UserSummaryResponse>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICacheService _cache;

    public GetUsersPagedQueryHandler(IApplicationDbContext context, ICacheService cache)
    {
        _context = context;
        _cache = cache;
    }

    public async Task<Result<PagedResult<UserSummaryResponse>>> Handle(
        GetUsersPagedQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Users.PagedList(request.Page, request.PageSize, request.SearchTerm);

        // Attempt cache-first retrieval
        var cached = await _cache.GetAsync<PagedResult<UserSummaryResponse>>(cacheKey, cancellationToken);
        if (cached is not null)
            return Result<PagedResult<UserSummaryResponse>>.Success(cached);

        var query = _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLower();
            query = query.Where(u =>
                u.FirstName.ToLower().Contains(term) ||
                u.LastName.ToLower().Contains(term) ||
                u.Email.Value.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(u => new UserSummaryResponse(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email.Value,
                u.Role.ToString(),
                u.CreatedAt))
            .ToListAsync(cancellationToken);

        var result = new PagedResult<UserSummaryResponse>(
            users, totalCount, request.Page, request.PageSize);

        await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), cancellationToken);

        return Result<PagedResult<UserSummaryResponse>>.Success(result);
    }
}