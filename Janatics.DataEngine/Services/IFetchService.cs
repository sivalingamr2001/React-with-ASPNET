using Janatics.DataEngine.Models;

namespace Janatics.DataEngine.Services;

/// <summary>
/// Executes dynamic fetch requests.
/// </summary>
public interface IFetchService
{
    /// <summary>
    /// Executes a read operation for a registered entity.
    /// </summary>
    Task<FetchResult> FetchAsync(FetchRequest request, CancellationToken cancellationToken = default);
}
