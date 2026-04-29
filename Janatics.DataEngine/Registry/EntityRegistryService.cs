using System.Data.Common;
using Dapper;
using Janatics.DataEngine.Exceptions;
using Janatics.DataEngine.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Janatics.DataEngine.Registry;

/// <summary>
/// Loads registry metadata from the metadata database and caches it for reuse.
/// </summary>
public sealed class EntityRegistryService : IEntityRegistryService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly EntityRegistryCache _cache;
    private readonly ILogger<EntityRegistryService> _logger;

    public EntityRegistryService(
        IDbConnectionFactory connectionFactory,
        EntityRegistryCache cache,
        ILogger<EntityRegistryService> logger)
    {
        _connectionFactory = connectionFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<EntityMeta> GetEntityAsync(string entityName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new EntityNotFoundException(entityName);
        }

        if (_cache.TryGetEntity(entityName, out var cachedEntity) && cachedEntity is not null)
        {
            _logger.LogDebug("DataEngine.CacheHit entity={Entity}", entityName);
            return cachedEntity;
        }

        _logger.LogDebug("DataEngine.CacheMiss entity={Entity}", entityName);

        await using DbConnection connection = await _connectionFactory.CreateOpenMetadataConnectionAsync(cancellationToken).ConfigureAwait(false);

        const string entitySql = """
            SELECT
                er.EntityId,
                er.ProfileId,
                er.EntityName,
                er.TableName,
                er.SchemaName,
                er.PkColumn,
                er.SoftDeleteColumn,
                er.IsReadOnly,
                er.AllowedRoles
            FROM EntityRegistry er
            WHERE LOWER(er.EntityName) = LOWER(@entityName);
            """;

        var entityRow = await connection.QuerySingleOrDefaultAsync<EntityRow>(
            new CommandDefinition(entitySql, new { entityName }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (entityRow is null)
        {
            throw new EntityNotFoundException(entityName);
        }

        const string columnSql = """
            SELECT
                cr.ColumnId,
                cr.EntityId,
                cr.ColumnName,
                cr.DataType,
                cr.IsRequired,
                cr.IsPrimaryKey,
                cr.IsSearchable,
                cr.IsReadOnly,
                cr.IsNullable,
                cr.OrdinalPosition
            FROM ColumnRegistry cr
            WHERE cr.EntityId = @entityId
            ORDER BY cr.OrdinalPosition, cr.ColumnName;
            """;

        var columns = (await connection.QueryAsync<ColumnRow>(
            new CommandDefinition(columnSql, new { entityId = entityRow.EntityId }, cancellationToken: cancellationToken)).ConfigureAwait(false))
            .Select(static column => new ColumnMeta(
                Guid.Parse(column.ColumnId),
                Guid.Parse(column.EntityId),
                column.ColumnName,
                column.DataType,
                column.IsRequired != 0,
                column.IsPrimaryKey != 0,
                column.IsSearchable != 0,
                column.IsReadOnly != 0,
                column.IsNullable != 0,
                checked((int)column.OrdinalPosition)))
            .ToList();

        var comparer = StringComparer.OrdinalIgnoreCase;
        var columnMap = columns.ToDictionary(column => column.ColumnName, comparer);
        var requiredColumns = new HashSet<string>(columns.Where(column => column.IsRequired).Select(column => column.ColumnName), comparer);
        var searchableColumns = new HashSet<string>(columns.Where(column => column.IsSearchable).Select(column => column.ColumnName), comparer);
        var roles = string.IsNullOrWhiteSpace(entityRow.AllowedRoles)
            ? new HashSet<string>(comparer)
            : new HashSet<string>(
                entityRow.AllowedRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                comparer);

        var entityMeta = new EntityMeta(
            Guid.Parse(entityRow.EntityId),
            Guid.Parse(entityRow.ProfileId),
            entityRow.EntityName,
            entityRow.TableName,
            entityRow.SchemaName,
            entityRow.PkColumn,
            entityRow.SoftDeleteColumn,
            entityRow.IsReadOnly != 0,
            roles,
            columnMap,
            requiredColumns,
            searchableColumns);

        _cache.SetEntity(entityMeta);
        return entityMeta;
    }

    public async Task<DbProfile> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetProfile(profileId, out var cachedProfile) && cachedProfile is not null)
        {
            _logger.LogDebug("DataEngine.CacheHit profile={ProfileId}", profileId);
            return cachedProfile;
        }

        _logger.LogDebug("DataEngine.CacheMiss profile={ProfileId}", profileId);

        await using DbConnection connection = await _connectionFactory.CreateOpenMetadataConnectionAsync(cancellationToken).ConfigureAwait(false);

        const string profileSql = """
            SELECT
                dp.ProfileId,
                dp.ProfileName,
                dp.Provider,
                dp.ConnectionString,
                dp.Status
            FROM DbProfiles dp
            WHERE dp.ProfileId = @profileId;
            """;

        var profileRow = await connection.QuerySingleOrDefaultAsync<ProfileRow>(
            new CommandDefinition(profileSql, new { profileId }, cancellationToken: cancellationToken)).ConfigureAwait(false);

        if (profileRow is null)
        {
            throw new DataEngineException($"Profile '{profileId}' was not found in the registry.", "PROFILE_NOT_FOUND");
        }

        var profile = new DbProfile(
            Guid.Parse(profileRow.ProfileId),
            profileRow.ProfileName,
            profileRow.Provider,
            profileRow.ConnectionString,
            profileRow.Status);

        _cache.SetProfile(profile);
        return profile;
    }

    public Task ClearCacheAsync() => _cache.ClearAsync();

    private sealed record EntityRow(
        string EntityId,
        string ProfileId,
        string EntityName,
        string TableName,
        string? SchemaName,
        string PkColumn,
        string? SoftDeleteColumn,
        long IsReadOnly,
        string? AllowedRoles);

    private sealed record ColumnRow(
        string ColumnId,
        string EntityId,
        string ColumnName,
        string DataType,
        long IsRequired,
        long IsPrimaryKey,
        long IsSearchable,
        long IsReadOnly,
        long IsNullable,
        long OrdinalPosition);

    private sealed record ProfileRow(
        string ProfileId,
        string ProfileName,
        string Provider,
        string ConnectionString,
        string Status);
}
