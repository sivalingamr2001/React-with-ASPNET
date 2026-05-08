using Janatics.DataEngine.Contracts.Requests;
using Janatics.DataEngine.TransactionEngine;
using Microsoft.AspNetCore.Mvc;

namespace Janatics.DataEngine.Api.Controllers;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionController(TransactionEngineService transactionEngineService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ExecuteAsync([FromBody] TransactionRequest request, CancellationToken ct)
    {
        var result = await transactionEngineService.ExecuteAsync(request, ct).ConfigureAwait(false);
        return result.Succeeded ? Ok(result) : BadRequest(result);
    }
}
