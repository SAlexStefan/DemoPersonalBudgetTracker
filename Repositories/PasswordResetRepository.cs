using Dapper;
using PersonalBudgetTrackerApi.Data;
using PersonalBudgetTrackerApi.Repositories.Interfaces;

namespace PersonalBudgetTrackerApi.Repositories;

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly DbConnectionFactory _dbConnection;

    public PasswordResetRepository(DbConnectionFactory dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(string hash)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @" SELECT password_reset_token_id as PasswordResetTokenId, token_hash as TokenHash, user_id as UserId, expires_at as ExpiresAt, used_at as UsedAt, created_at as CreatedAt
                    FROM    password_reset_tokens
                    WHERE   token_hash = @TokenHash";

        var passwordResetToken = await connection.QueryFirstOrDefaultAsync<PasswordResetToken>(sql, new{TokenHash = hash});

        return passwordResetToken;
    }

    public async Task<PasswordResetToken> CreatePasswordResetTokenAsync(PasswordResetToken passwordResetToken)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"INSERT INTO password_reset_tokens(token_hash, user_id, expires_at)
                    VALUES(@TokenHash, @UserId, @ExpiresAt)
                    RETURNING password_reset_token_id as PasswordResetTokenId";

        var inserted = await connection.QuerySingleAsync<int>(sql, new
        {
            passwordResetToken.TokenHash,
            passwordResetToken.UserId,
            passwordResetToken.ExpiresAt
        });
        passwordResetToken.PasswordResetTokenId = inserted;

        return passwordResetToken;
    }

    public async Task MarkedUsedPasswordResetTokenAsync(int passwordResetTokenId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"UPDATE password_reset_tokens
                    SET    used_at = (now() AT TIME ZONE 'utc')
                    WHERE  password_reset_token_id = @PasswordResetTokenId";

        await connection.ExecuteAsync(sql, new { PasswordResetTokenId = passwordResetTokenId });
    }
}
