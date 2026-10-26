namespace PersonalBudgetTrackerApi.Models;

public class Transaction
{
    public int TransactionId { get; set; }
    public Guid PublicId { get; set; }
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string? Note { get; set; }
    public required Category Category { get; set; }
    public int UserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
