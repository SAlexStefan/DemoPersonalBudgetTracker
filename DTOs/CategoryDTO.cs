using System.ComponentModel.DataAnnotations;

using PersonalBudgetTrackerApi.Models;

namespace PersonalBudgetTrackerApi.DTOs.Category;

public record CategoryCreateRequest([Required, MaxLength(255)]string Name, CategoryType Type);
public record CategoryResponse(Guid PublicId, string Name, CategoryType Type, bool IsDefault);

