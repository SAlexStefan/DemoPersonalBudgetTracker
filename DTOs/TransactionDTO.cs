using System.ComponentModel.DataAnnotations;

namespace PersonalBudgetTrackerApi.DTOs;

public record TransactionCreateRequest(
    [Range(0.01, 999999999999999.99)]decimal Amount, 
    DateTime TransactionDate, 
    [MaxLength(500)]string? Note, 
    Guid PublicCategoryId);
public record TransactionUpdateRequest(
    [Range(0.01, 999999999999999.99)]decimal Amount, 
    DateTime TransactionDate, 
    [MaxLength(500)]string? Note, 
    Guid PublicCategoryId);
public record TransactionResponse(
    Guid PublicId,
    decimal Amount,
    DateTime TransactionDate,
    String? Note,
    Guid PublicCategoryId,
    string CategoryName
);
