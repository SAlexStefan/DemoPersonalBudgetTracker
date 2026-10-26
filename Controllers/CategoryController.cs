using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PersonalBudgetTrackerApi.DTOs.Category;
using PersonalBudgetTrackerApi.Interfaces;
using PersonalBudgetTrackerApi.Models;
using PersonalBudgetTrackerApi.Repositories.Interfaces;

namespace PersonalBudgetTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoryController : ControllerBase
{
    private readonly ICategoryRepositories _categoryRepository;
    private readonly IUserRepository _userRepository;

    public CategoryController(ICategoryRepositories categoryRepository, IUserRepository userRepository)
    {
        _categoryRepository = categoryRepository;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryResponse>>> GetCategories()
    {
        var publicUserId = this.GetUserPublicId();
        var userId = await _userRepository.GetUserIdByPublic(publicUserId);
        if (userId is null)
        {
            return Forbid("User is not authorised for this action.");
        }

        var categories = await _categoryRepository.GetCategoriesForUser((int)userId);

        var response = categories.Select(c => new CategoryResponse(c.PublicId, c.Name, c.Type, c.IsDefault));
        return Ok(response);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryResponse>> CreateCategory(CategoryCreateRequest request)
    {
        var publicUserId = this.GetUserPublicId();
        var userId = await _userRepository.GetUserIdByPublic(publicUserId);
        if (userId is null)
        {
            return Forbid("User is not authorised for this action.");
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest("Category name is required");
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Type = request.Type,
            UserId = userId,
            IsDefault = false
        };

        category = await _categoryRepository.AddCategoryAsync(category);
        return Ok(new CategoryResponse(category.PublicId, category.Name, category.Type, category.IsDefault));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCategory(Guid id)
    {
        var publicUserId = this.GetUserPublicId();
        var userId = await _userRepository.GetUserIdByPublic(publicUserId);
        var category = await _categoryRepository.GetCategoryByPublicId(id);
        if (userId is null || category is null || category.UserId != userId)
        {
            return Forbid("User is not authorised for this action.");
        }

        var isCategoryUsed = await _categoryRepository.IsCategoryInUse(category.CategoryId);
        if (isCategoryUsed)
        {
            return BadRequest("Cannot delete a category which is used.");
        }

        await _categoryRepository.DeleteCategoryAsync(category.CategoryId);
        return NoContent();
    }
}
