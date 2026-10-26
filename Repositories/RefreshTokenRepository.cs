using Dapper;
using PersonalBudgetTrackerApi.Data;
using PersonalBudgetTrackerApi.Models;
using PersonalBudgetTrackerApi.Repositories.Interfaces;

namespace PersonalBudgetTrackerApi.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly DbConnectionFactory _dbConnection;
    private record InsertResult(int RefreshTokenId, Guid PublicId);

    public RefreshTokenRepository(DbConnectionFactory dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<RefreshToken> CreateTokenAsync(RefreshToken token)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"INSERT INTO refresh_tokens(token_hash, user_id, expires_at)
                    VALUES(@TokenHash, @UserId, @ExpiresAt)
                    RETURNING refresh_token_id as RefreshTokenId, public_id as PublicId";

        var inserted = await connection.QuerySingleAsync<InsertResult>(sql, new
        {
            token.TokenHash,
            token.UserId,
            token.ExpiresAt
        });

        token.RefreshTokenId = inserted.RefreshTokenId;
        token.PublicId = inserted.PublicId;
        return token;
    }

    public async Task<RefreshToken?> GetTokenByPublicIdAsync(Guid publicId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"SELECT refresh_token_id as RefreshTokenId, public_id as PublicId, token_hash as TokenHash,
                           user_id as UserId, expires_at as ExpiresAt, revoked_at as RevokedAt,
                           replaced_by_public_id as ReplacedByPublicId, created_at as CreatedAt
                    FROM   refresh_tokens
                    WHERE  public_id = @PublicId";

        return await connection.QuerySingleOrDefaultAsync<RefreshToken>(sql, new { PublicId = publicId });
    }

    public async Task<RefreshToken?> GetTokenByHashAsync(string tokenHash)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"SELECT refresh_token_id as RefreshTokenId, public_id as PublicId, token_hash as TokenHash,
                           user_id as UserId, expires_at as ExpiresAt, revoked_at as RevokedAt,
                           replaced_by_public_id as ReplacedByPublicId, created_at as CreatedAt
                    FROM   refresh_tokens
                    WHERE  token_hash = @TokenHash";

        return await connection.QuerySingleOrDefaultAsync<RefreshToken>(sql, new { TokenHash = tokenHash });
    }

    public async Task RevokeTokenAsync(int refreshTokenId, Guid? replacedByPublicId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"UPDATE refresh_tokens
                    SET    revoked_at = (now() AT TIME ZONE 'utc'), replaced_by_public_id = @ReplacedByPublicId
                    WHERE  refresh_token_id = @RefreshTokenId";

        await connection.ExecuteAsync(sql, new { RefreshTokenId = refreshTokenId, ReplacedByPublicId = replacedByPublicId });
    }

    public async Task RevokeAllTokensForUserAsync(int userId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"UPDATE refresh_tokens
                    SET    revoked_at = (now() AT TIME ZONE 'utc')
                    WHERE  user_id = @UserId AND revoked_at IS NULL";

        await connection.ExecuteAsync(sql, new { UserId = userId });
    }
}
