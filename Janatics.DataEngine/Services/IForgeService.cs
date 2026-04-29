using Janatics.DataEngine.Models;

namespace Janatics.DataEngine.Services;

/// <summary>
/// Executes dynamic forge requests.
/// </summary>
public interface IForgeService
{
    /// <summary>
    /// Executes a write operation for a registered entity.
    /// </summary>
    Task<ForgeResult> ForgeAsync(ForgeRequest request, CancellationToken cancellationToken = default);
}
