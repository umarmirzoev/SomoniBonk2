using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Enums;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class SavingsGoalService : ISavingsGoalService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SavingsGoalService> _logger;

    public SavingsGoalService(AppDbContext db, INotificationService notificationService, ILogger<SavingsGoalService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Response<SavingsGoalGetDto>> CreateAsync(Guid userId, SavingsGoalInsertDto dto)
    {
        try
        {
            if (dto.TargetAmount <= 0)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.BadRequest, "Target amount must be greater than zero");

            if (dto.Deadline <= DateTime.UtcNow)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.BadRequest, "Deadline must be in the future");

            if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
                return new Response<SavingsGoalGetDto>(HttpStatusCode.BadRequest, "Invalid currency");

            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.NotFound, "Account not found");

            if (account.Currency != currency)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.BadRequest, "Goal currency must match account currency");

            var goal = new SavingsGoal
            {
                UserId = userId,
                AccountId = account.Id,
                Name = dto.Name,
                TargetAmount = dto.TargetAmount,
                CurrentAmount = 0,
                Currency = currency,
                Deadline = dto.Deadline,
                IsCompleted = false
            };

            _db.SavingsGoals.Add(goal);
            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "Savings goal", $"Savings goal '{goal.Name}' created successfully.", "SavingsGoal");

            return new Response<SavingsGoalGetDto>(HttpStatusCode.OK, "Savings goal created successfully", MapToDto(goal, account.AccountNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create savings goal failed");
            return new Response<SavingsGoalGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<SavingsGoalGetDto>> GetMyGoalsAsync(Guid userId, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var query = _db.SavingsGoals.AsNoTracking()
            .Include(x => x.Account)
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SavingsGoalGetDto>
        {
            Items = items.Select(x => MapToDto(x, x.Account.AccountNumber)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<SavingsGoalGetDto>> DepositToGoalAsync(Guid userId, Guid goalId, SavingsGoalDepositDto dto)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (dto.Amount <= 0)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.BadRequest, "Amount must be greater than zero");

            var goal = await _db.SavingsGoals
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.Id == goalId && x.UserId == userId);
            if (goal == null)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.NotFound, "Savings goal not found");

            if (goal.IsCompleted)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.BadRequest, "Savings goal is already completed");

            if (!goal.Account.IsActive)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.BadRequest, "Linked account is inactive");

            if (goal.Account.Balance < dto.Amount)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.BadRequest, "Insufficient funds");

            goal.Account.Balance -= dto.Amount;
            goal.CurrentAmount += dto.Amount;
            if (goal.CurrentAmount >= goal.TargetAmount)
                goal.IsCompleted = true;

            _db.Transactions.Add(new Transaction
            {
                FromAccountId = goal.AccountId,
                Amount = dto.Amount,
                Currency = goal.Currency,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Completed,
                Description = dto.Description ?? $"Deposit to savings goal '{goal.Name}'"
            });

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "Savings goal deposit", $"{dto.Amount} {goal.Currency} added to '{goal.Name}'.", "SavingsGoal");
            await dbTransaction.CommitAsync();

            return new Response<SavingsGoalGetDto>(HttpStatusCode.OK, "Deposit to savings goal completed", MapToDto(goal, goal.Account.AccountNumber));
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Deposit to savings goal failed");
            return new Response<SavingsGoalGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<SavingsGoalGetDto>> CompleteGoalAsync(Guid userId, Guid goalId)
    {
        try
        {
            var goal = await _db.SavingsGoals
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.Id == goalId && x.UserId == userId);
            if (goal == null)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.NotFound, "Savings goal not found");

            if (goal.CurrentAmount < goal.TargetAmount)
                return new Response<SavingsGoalGetDto>(HttpStatusCode.BadRequest, "Target amount has not been reached yet");

            goal.IsCompleted = true;
            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "Savings goal completed", $"Savings goal '{goal.Name}' has been completed.", "SavingsGoal");

            return new Response<SavingsGoalGetDto>(HttpStatusCode.OK, "Savings goal completed successfully", MapToDto(goal, goal.Account.AccountNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Complete savings goal failed");
            return new Response<SavingsGoalGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private static SavingsGoalGetDto MapToDto(SavingsGoal goal, string accountNumber) => new()
    {
        Id = goal.Id,
        UserId = goal.UserId,
        AccountId = goal.AccountId,
        AccountNumber = accountNumber,
        Name = goal.Name,
        TargetAmount = goal.TargetAmount,
        CurrentAmount = goal.CurrentAmount,
        Currency = goal.Currency.ToString(),
        Deadline = goal.Deadline,
        IsCompleted = goal.IsCompleted,
        CreatedAt = goal.CreatedAt
    };
}
