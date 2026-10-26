using PersonalBudgetTrackerApi.Models;

namespace PersonalBudgetTrackerApi.Interfaces;

public interface ICategoryRepositories
{
    Task<List<Category>> GetCategoriesForUser(int userId);
    Task<Category?> GetCategoryByPublicId(Guid publicId);
    Task<bool> IsCategoryInUse(int categoryId);
    Task<Category> AddCategoryAsync(Category category);
    Task DeleteCategoryAsync(int categoryId);
}
