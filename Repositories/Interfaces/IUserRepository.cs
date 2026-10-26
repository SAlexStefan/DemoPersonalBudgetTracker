using PersonalBudgetTrackerApi.Models;

namespace PersonalBudgetTrackerApi.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User> CreateUserasync(User user);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User?> GetUserByIdAsync(int userId);
    Task<int?> GetUserIdByPublic(Guid guid);
    Task RecordFailedLoginAsync(int userId, int failedAttempts, DateTime? lockedUntil);
    Task ResetFailedLoginAsync(int userId);
    Task UpdatePasswordAsync(int userId, string newHash);
}
