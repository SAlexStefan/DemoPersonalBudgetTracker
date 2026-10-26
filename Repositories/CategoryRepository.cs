using Dapper;
using PersonalBudgetTrackerApi.Data;
using PersonalBudgetTrackerApi.Interfaces;
using PersonalBudgetTrackerApi.Models;

namespace PersonalBudgetTrackerApi;

public class CategoryRepository : ICategoryRepositories
{
    private readonly DbConnectionFactory _dbConnection;
    private record InsertResult(int CategoryId, Guid PublicId);

    public CategoryRepository(DbConnectionFactory dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<List<Category>> GetCategoriesForUser(int userId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"SELECT  category_id as CategoryId, public_id as PublicId, name as Name, type as Type, is_default as IsDefault, user_id as UserId, created_at as CreatedAt
                    FROM    categories 
                    WHERE   user_id IS NULL or user_id = @UserId";

        var categories = await connection.QueryAsync<Category>(sql, new { UserId = userId });
        return [.. categories];
    }

    public async Task<Category?> GetCategoryByPublicId(Guid publicId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"SELECT category_id as CategoryId, public_id as PublicId, name as Name, type as Type, is_default as IsDefault, user_id as UserId
                    FROM    categories
                    WHERE   public_id = @PublicId";
        var category = await connection.QueryFirstOrDefaultAsync<Category>(sql, new
        {
            PublicId = publicId
        });
        return category;
    }

    public async Task<bool> IsCategoryInUse(int categoryId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"SELECT COUNT(1) FROM transactions WHERE category_id = @CategoryId";
        var count = await connection.ExecuteScalarAsync<int>(sql, new {CategoryId = categoryId});
        return count > 0;
    }

    public async Task<Category> AddCategoryAsync(Category category)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"INSERT INTO categories(name, type, is_default, user_id)
                    VALUES(@Name, @Type, @IsDefault, @UserId)
                    RETURNING category_id as CategoryId, public_id as PublicId";

        var newCategory = await connection.QuerySingleAsync<InsertResult>(sql, new
        {
            category.Name,
            Type = category.Type.ToString(),
            category.IsDefault,
            category.UserId
        });     
        category.CategoryId = newCategory.CategoryId;
        category.PublicId = newCategory.PublicId;

        return category;
    }

    public async Task DeleteCategoryAsync(int categoryId)
    {
        using var connection = _dbConnection.CreateConnection();
        var sql = @"DELETE FROM categories WHERE category_id = @CategoryId";
        await connection.ExecuteAsync(sql, new{CategoryId = categoryId});
    }
}
