using Janatics.DataEngine.Contracts.Requests;
using Janatics.DataEngine.QueryEngine;
using Microsoft.AspNetCore.Mvc;

namespace Janatics.DataEngine.Api.Controllers;

[ApiController]
[Route("api/fetch")]
public sealed class FetchController(DataEngineQueryService queryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ExecuteAsync([FromBody] FetchRequest request, CancellationToken ct)
    {
        var result = await queryService.ExecuteAsync(request, ct).ConfigureAwait(false);
        return Ok(result);
    }
}
