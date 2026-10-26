using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using PersonalBudgetTrackerApi.DTOs;
using PersonalBudgetTrackerApi.Interfaces;
using PersonalBudgetTrackerApi.Models;
using PersonalBudgetTrackerApi.Repositories.Interfaces;

namespace PersonalBudgetTrackerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionController : ControllerBase
{
    private readonly ITransactionRepository _transactionRepository;
    private readonly ICategoryRepositories _categoryRepository;
    private readonly IUserRepository _userRepository;

    public TransactionController(ITransactionRepository transactionRepository, ICategoryRepositories categoryRepositories, IUserRepository userRepository)
    {
        _transactionRepository = transactionRepository;
        _categoryRepository = categoryRepositories;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<TransactionResponse>>> GetAllTransactions([FromQuery] int? year, [FromQuery] int? month, [FromQuery] Guid categoryId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var publicUserId = this.GetUserPublicId();
        var userId = await _userRepository.GetUserIdByPublic(publicUserId);
        var category = await _categoryRepository.GetCategoryByPublicId(categoryId);
        if (userId is null || (category is not null && !category.IsDefault && category.UserId != userId))
        {
            return Forbid("User is not authorised for this action.");
        }

        if (page < 1 || pageSize < 1 || pageSize > 100)
            return BadRequest("Incorrect paging.");

        var offset = (page - 1) * pageSize;
        var transactions = await _transactionRepository.GetTransactionsForUserAsync((int)userId, year, month, category?.CategoryId, pageSize, offset);

        var result = transactions.Select(t => new TransactionResponse(t.PublicId, t.Amount, t.TransactionDate, t.Note, t.Category.PublicId, t.Category.Name)).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TransactionResponse>> GetTransaction(Guid id)
    {
        var publicUserId = this.GetUserPublicId();
        var userId = await _userRepository.GetUserIdByPublic(publicUserId);
        var transaction = await _transactionRepository.GetTransactionByIdAsync(id);

        if (userId is null || (transaction is not null && transaction.UserId != userId))
        {
            return Forbid("User is not authorised for this action.");
        }

        if (transaction is null)
        {
            return NotFound();
        }

        return Ok(new TransactionResponse(transaction.PublicId, transaction.Amount, transaction.TransactionDate, transaction.Note, transaction.Category.PublicId, transaction.Category.Name));

    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> CreateTransaction(TransactionCreateRequest request)
    {
        var publicUserId = this.GetUserPublicId();
        var userId = await _userRepository.GetUserIdByPublic(publicUserId);

        var category = await _categoryRepository.GetCategoryByPublicId(request.PublicCategoryId);

        if (userId is null)
        {
            return Forbid("User is not authorised for this action.");
        }

        if (category is null || (!category.IsDefault && category.UserId != userId))
        {
            return BadRequest("Invalid Category");
        }

        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than 0");
        }

        var transaction = new Transaction
        {
            Amount = request.Amount,
            TransactionDate = request.TransactionDate,
            Note = request.Note,
            Category = category,
            UserId = (int)userId
        };

        transaction = await _transactionRepository.CreateTransactionAsync(transaction);

        return Ok(new TransactionResponse(transaction.PublicId, transaction.Amount, transaction.TransactionDate, transaction.Note, transaction.Category.PublicId, transaction.Category.Name));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<TransactionResponse>> UpdateTransaction(Guid id, TransactionUpdateRequest request)
    {
        var publicUserId = this.GetUserPublicId();
        var userId = await _userRepository.GetUserIdByPublic(publicUserId);

        var existingTransaction = await _transactionRepository.GetTransactionByIdAsync(id);

        if (existingTransaction is null)
        {
            return NotFound();
        }

        if (userId is null || existingTransaction.UserId != userId)
        {
            return Forbid("User is not authorised for this action.");
        }

        if (request.Amount <= 0)
        {
            return BadRequest("Amount must be greater than 0");
        }

        var category = await _categoryRepository.GetCategoryByPublicId(request.PublicCategoryId);
        if (category is null || (!category.IsDefault && category.UserId != userId))
        {
            return BadRequest("Invalid Category");
        }

        var transaction = new Transaction
        {
            TransactionId = existingTransaction.TransactionId,
            PublicId = existingTransaction.PublicId,
            Amount = request.Amount,
            TransactionDate = request.TransactionDate,
            Note = request.Note,
            Category = category,
            UserId = (int)userId
        };

        var hasUpdated = await _transactionRepository.UpdateTransactionAsync(transaction);
        if (!hasUpdated)
        {
            return NotFound();
        }

        return Ok(new TransactionResponse(transaction.PublicId, transaction.Amount, transaction.TransactionDate, transaction.Note, transaction.Category.PublicId, transaction.Category.Name));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTransaction(Guid id)
    {
        var publicUserId = this.GetUserPublicId();
        var userId = await _userRepository.GetUserIdByPublic(publicUserId);
        var existingTransaction = await _transactionRepository.GetTransactionByIdAsync(id);

        if (existingTransaction is null)
        {
            return NotFound();
        }

        if (userId is null || existingTransaction.UserId != userId)
        {
            return Forbid("User is not authorised for this action.");
        }

        var deleteResult = await _transactionRepository.DeleteAsync(existingTransaction.TransactionId, (int)userId);

        if (!deleteResult)
        {
            return NotFound();
        }

        return NoContent();
    }

}
