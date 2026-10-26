using PersonalBudgetTrackerApi.Data;
using PersonalBudgetTrackerApi.Models;
using Dapper;
using PersonalBudgetTrackerApi.Repositories.Interfaces;

namespace PersonalBudgetTrackerApi.Repositories;

public class UserRepository : IUserRepository
{
    private readonly DbConnectionFactory _dbConnection;

    public UserRepository(DbConnectionFactory dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = "SELECT user_id as UserId, public_id AS PublicId, email, password_hash as PasswordHash, failed_login_attempts as FailedLoginAttempts, locked_until as LockedUntil, created_at as CreatedAt  FROM Users WHERE email = @email";

        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { email = email });
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = "SELECT user_id as UserId, public_id AS PublicId, email as Email, password_hash as PasswordHash, failed_login_attempts as FailedLoginAttempts, locked_until as LockedUntil, created_at as CreatedAt  FROM Users WHERE user_id = @UserId";

        return await connection.QuerySingleOrDefaultAsync<User>(sql, new { UserId = userId });
    }

    public async Task<User> CreateUserasync(User user)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"INSERT INTO Users(email, password_hash)
                    VALUES(@Email, @PasswordHash)
                    RETURNING user_id as UserId, public_id as PublicId";
        var inserted = await connection.QuerySingleAsync<(int UserId, Guid PublicId)>(sql, new
        {
            Email = user.Email,
            PasswordHash = user.PasswordHash
        });

        user.UserId = inserted.UserId;
        user.PublicId = inserted.PublicId;
        return user;
    }

    public async Task<int?> GetUserIdByPublic(Guid guid)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"SELECT user_id FROM users WHERE public_id = @publicId";
        var userId = await connection.QuerySingleOrDefaultAsync<int?>(sql, new
        {
            publicId = guid
        });

        return userId;
    }

    public async Task RecordFailedLoginAsync(int userId, int failedAttempts, DateTime? lockedUntil)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"UPDATE users SET failed_login_attempts = @FailedAttempts, locked_until = @LockedUntil
                WHERE user_id = @UserId";
        await connection.ExecuteAsync(sql, new { FailedAttempts = failedAttempts, LockedUntil = lockedUntil, UserId = userId });
    }

    public async Task ResetFailedLoginAsync(int userId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"UPDATE users SET failed_login_attempts = 0, locked_until = NULL WHERE user_id = @UserId";
        await connection.ExecuteAsync(sql, new { UserId = userId });
    }
    public async Task UpdatePasswordAsync(int userId, string newHash)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"UPDATE users SET password_hash = @NewHash WHERE user_id = @UserId";
        await connection.ExecuteAsync(sql, new { NewHash = newHash, UserId = userId });
    }

}
