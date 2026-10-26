namespace PersonalBudgetTrackerApi.Repositories.Interfaces;

public interface IPasswordResetRepository
{
    Task<PasswordResetToken?> GetPasswordResetTokenByHashAsync(string hash);
    Task<PasswordResetToken> CreatePasswordResetTokenAsync(PasswordResetToken passwordResetToken);
    Task MarkedUsedPasswordResetTokenAsync(int passwordResetTokenId);
}
