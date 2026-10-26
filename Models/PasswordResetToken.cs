namespace PersonalBudgetTrackerApi;

public class PasswordResetToken
{
    public int PasswordResetTokenId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
