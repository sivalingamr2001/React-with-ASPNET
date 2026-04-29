namespace Janatics.DataEngine;

/// <summary>
/// Configures registry connectivity and engine behaviour.
/// </summary>
public sealed class DataEngineOptions
{
    /// <summary>
    /// Gets or sets the metadata registry provider name.
    /// </summary>
    public string MetadataProvider { get; set; } = "Sqlite";

    /// <summary>
    /// Gets or sets the metadata registry connection string.
    /// </summary>
    public string MetadataConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the in-memory registry cache duration.
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or sets the maximum supported page size.
    /// </summary>
    public int MaxPageSize { get; set; } = 1000;
}
