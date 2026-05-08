using Janatics.DataEngine.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Janatics.DataEngine.Api.Controllers;

[ApiController]
[Route("api/metadata")]
public sealed class MetadataController(IMetadataRepository metadataRepository) : ControllerBase
{
    [HttpGet("entities/{tenantCode}")]
    public async Task<IActionResult> GetEntitiesAsync(string tenantCode, CancellationToken ct)
    {
        var result = await metadataRepository.GetAllEntitiesAsync(tenantCode, ct).ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("queries/{tenantCode}")]
    public async Task<IActionResult> GetQueriesAsync(string tenantCode, CancellationToken ct)
    {
        var result = await metadataRepository.GetQueryDefinitionsAsync(tenantCode, ct).ConfigureAwait(false);
        return Ok(result);
    }
}
