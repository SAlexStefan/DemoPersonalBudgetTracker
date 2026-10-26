using PersonalBudgetTrackerApi.Models;

namespace PersonalBudgetTrackerApi.Repositories.Interfaces;

public interface ITransactionRepository
{
    Task<List<Transaction>> GetTransactionsForUserAsync(int userId, int? year, int? month, int? categoryId, int pageSize, int offset);
    Task<Transaction?> GetTransactionByIdAsync(Guid publicId);
    Task<Transaction> CreateTransactionAsync(Transaction transaction);
    Task<bool> UpdateTransactionAsync(Transaction transaction);
    Task<bool> DeleteAsync(int transactionId, int userId);
}
