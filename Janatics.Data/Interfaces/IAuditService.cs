using System.Data;
using KATCRUDServices.Core.Models;

namespace KATCRUDServices.Core.Interfaces;

public interface IAuditService
{
    /// <summary>
    /// Enqueues an audit for the current transaction. When transaction is not null, the job is cached;
    /// no write to the auditlog table occurs until after commit (see FlushAuditLogsFireAndForget).
    /// For UPDATE/DELETE, pass beforeData when you already have the row state so no read runs during the transaction.
    /// </summary>
    Task<bool> TryAuditAsync(
        string entityName,
        Guid transactionId,
        AuditOperation operation,
        string modifiedBy,
        Dictionary<string, object?>? extendedProperties,
        IDbConnection connection,
        IDbTransaction? transaction = null,
        Dictionary<string, object?>? beforeData = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// All auditlog processing (build changelog + INSERT into auditlog) runs here, on a new connection, after commit.
    /// </summary>
    void FlushAuditLogsFireAndForget(string connectionString);

    /// <summary>
    /// When passing a list, flushes that list (used when the caller holds the audit jobs from the transaction scope).
    /// When null, flushes from the current AsyncLocal cache.
    /// </summary>
    void FlushAuditLogsFireAndForget(string connectionString, List<AuditJob>? jobs);

    /// <summary>
    /// Prepares the per-request audit cache so jobs added during the transaction are stored in the returned list.
    /// Call at the start of the transaction scope; pass the returned list to FlushAuditLogsFireAndForget after commit.
    /// </summary>
    List<AuditJob> PrepareAuditCacheForRequest();

    /// <summary>
    /// Reads the current row for a given table and id (used for UPDATE/DELETE before-data in audit).
    /// When transaction is provided, uses that connection/transaction; otherwise opens a new connection.
    /// </summary>
    Task<Dictionary<string, object?>?> ReadExistingDataAsync(
        string tableName,
        Guid transactionId,
        IDbConnection connection,
        IDbTransaction? transaction = null,
        CancellationToken cancellationToken = default,
        string idColumnName = "id");
}
