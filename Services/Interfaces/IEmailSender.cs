namespace PersonalBudgetTrackerApi.Services.Interfaces;

public interface IEmailSender
{
    Task SendPasswordResetAsync(string toEmail, string resetLink);
}
