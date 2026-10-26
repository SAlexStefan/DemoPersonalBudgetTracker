using System.Runtime.InteropServices;

namespace PersonalBudgetTrackerApi.Models;

public class RefreshToken
{
    public int RefreshTokenId { get; set; }
    public Guid PublicId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public int UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public Guid? ReplacedByPublicId { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.Now;

}
