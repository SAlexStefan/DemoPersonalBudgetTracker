using Dapper;
using Microsoft.Identity.Client;
using PersonalBudgetTrackerApi.Data;
using PersonalBudgetTrackerApi.Models;
using PersonalBudgetTrackerApi.Repositories.Interfaces;

namespace PersonalBudgetTrackerApi;

public class TransactionRepository : ITransactionRepository
{
    private readonly DbConnectionFactory _dbConnection;

    private record InsertResult(int TransactionId, Guid PublicId);
    
    public TransactionRepository(DbConnectionFactory dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<List<Transaction>> GetTransactionsForUserAsync(int userId, int? year, int? month, int? categoryId, int pageSize, int offset)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"SELECT  t.transaction_id as TransactionId,t.public_id as PublicId, t.amount as Amount, t.transaction_date as TransactionDate, t.note as Note, t.user_id as UserId, t.created_at as CreatedAt, c.category_id as CategoryId, c.public_id as PublicId, c.name as Name, c.type as Type
                    FROM    transactions t 
                            LEFT OUTER JOIN categories c
                            ON t.category_id = c.category_id
                    WHERE   t.user_id = @UserId
                            AND(@Year IS NULL OR EXTRACT(YEAR FROM t.transaction_date) = @Year)
                            AND(@Month IS NULL OR EXTRACT(MONTH FROM t.transaction_date) = @Month)
                            AND(@CategoryId IS NULL OR t.category_id = @CategoryId)
                    ORDER BY t.transaction_date DESC, t.transaction_id DESC
                    LIMIT @PageSize OFFSET @Offset;        ";

        var transactions = await connection.QueryAsync<Transaction, Category, Transaction>(sql,
        map: (transaction, category) =>
        {
            transaction.Category = category;
            return transaction;
        },
        param: new {UserId = userId, Year = year, Month = month, CategoryId = categoryId, PageSize = pageSize, Offset = offset},
        splitOn: "CategoryId"
        );

        return [.. transactions];
    }

    public async Task<Transaction?> GetTransactionByIdAsync(Guid publicId)
    {
        using  var connection = _dbConnection.CreateConnection();
        var sql = @"SELECT  t.transaction_id as TransactionId,t.public_id as PublicId, t.amount as Amount, t.transaction_date as TransactionDate, t.note as Note, t.user_id as UserId, t.created_at as CreatedAt, c.category_id as CategoryId, c.public_id as PublicId, c.name as Name, c.type as Type
                    FROM    transactions t 
                            LEFT OUTER JOIN categories c
                            ON t.category_id = c.category_id
                    WHERE   t.public_id = @PublicId";

        var result = await connection.QueryAsync<Transaction, Category, Transaction>(
        sql,
        map: (transaction, category) =>
        {
            transaction.Category = category;
            return transaction;
        },
        param: new{PublicId = publicId},
        splitOn: "CategoryId");

        return result.FirstOrDefault();
    }

    public async Task<Transaction> CreateTransactionAsync(Transaction transaction)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"INSERT INTO transactions(amount, transaction_date, note, user_id, category_id)
                    VALUES(@Amount, @TransactionDate, @Note, @UserId, @CategoryId)
                    RETURNING transaction_id as TransactionId, public_id as PublicId";

        var newTransaction = await connection.QuerySingleAsync<InsertResult>(sql, new
        {
            transaction.Amount,
            transaction.TransactionDate,
            transaction.Note,
            transaction.UserId,
            transaction.Category.CategoryId
        });

        transaction.TransactionId = newTransaction.TransactionId;
        transaction.PublicId = newTransaction.PublicId;

        return transaction;     
    }

    public async Task<bool> UpdateTransactionAsync(Transaction transaction)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"UPDATE  transactions
                    SET     amount = @Amount,
                            transaction_date = @TransactionDate,
                            note = @Note,
                            category_id = @CategoryId
                    WHERE   transaction_id = @TransactionId
                            AND user_id = @UserId
                    ";

        var rowsUpdated = await connection.ExecuteAsync(sql, new
        {
            transaction.Amount,
            transaction.TransactionDate,
            transaction.Note,
            transaction.Category.CategoryId,
            transaction.TransactionId,
            transaction.UserId
        });  

        return rowsUpdated > 0;          
    }

    public async Task<bool> DeleteAsync(int transactionId, int userId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"DELETE FROM transactions WHERE transaction_id = @TransactionId AND user_id = @UserId";

        var rowsDeleted = await connection.ExecuteAsync(sql, new{ TransactionId = transactionId, UserId = userId});

        return rowsDeleted > 0;
    }
}
