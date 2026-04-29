namespace Janatics.DataEngine.Registry;

/// <summary>
/// Represents a registry-managed database connection profile.
/// </summary>
public sealed record DbProfile(
    Guid ProfileId,
    string ProfileName,
    string Provider,
    string ConnectionString,
    string Status);
