namespace Janatics.DataEngine.Registry;

/// <summary>
/// Resolves entity metadata and database profiles from the registry database.
/// </summary>
public interface IEntityRegistryService
{
    /// <summary>
    /// Resolves an entity by its registry alias.
    /// </summary>
    Task<EntityMeta> GetEntityAsync(string entityName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves a database profile by identifier.
    /// </summary>
    Task<DbProfile> GetProfileAsync(Guid profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes cached registry state.
    /// </summary>
    Task ClearCacheAsync();
}
