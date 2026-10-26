using PersonalBudgetTrackerApi.Models;

namespace PersonalBudgetTrackerApi.Repositories.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken> CreateTokenAsync(RefreshToken token);
    Task<RefreshToken?> GetTokenByPublicIdAsync(Guid publicId);
    Task<RefreshToken?> GetTokenByHashAsync(string tokenHash);
    Task RevokeTokenAsync(int refreshTokenId, Guid? replacedByPublicId);
    Task RevokeAllTokensForUserAsync(int userId);
}
