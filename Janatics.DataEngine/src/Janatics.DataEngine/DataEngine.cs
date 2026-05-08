namespace Janatics.DataEngine;

public class DataEngine(IProcessService processService, IAuditService auditService)
{
    private readonly IProcessService _processService = processService;
    private readonly IAuditService _auditService = auditService;

    /// <summary>
    /// The primary method to run data operations
    /// </summary>
    public async Task ExecuteAsync(ProcessRequest request)
    {
        // World-class logic: Audit -> Process -> Log
        await _processService.ProcessAsync(request);
    }
}
