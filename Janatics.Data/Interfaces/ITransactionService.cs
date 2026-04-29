using KATCRUDServices.Core.Models;

namespace KATCRUDServices.Core.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResult> ProcessTransactionAsync(TransactionRequest request);
        Task<BulkTransactionResult> ProcessBulkTransactionAsync(BulkTransactionRequest request);
    }
}