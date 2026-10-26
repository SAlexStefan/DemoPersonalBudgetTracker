using PersonalBudgetTrackerApi.Services;
using PersonalBudgetTrackerApi.Services.Interfaces;

namespace PersonalBudgetTrackerApi;

public class EmailSender : IEmailSender
{
    private readonly ILogger<EmailSender> _logger;
    public EmailSender(ILogger<EmailSender> logger) => _logger = logger;

    public Task SendPasswordResetAsync(string toEmail, string resetLink)
    {
        _logger.LogInformation("Password reset link for {Email}: {Link}", toEmail, resetLink);
        return Task.CompletedTask;
    }
}
