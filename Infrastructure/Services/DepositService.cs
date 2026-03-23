using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Enums;
using SomoniBank.Domain.Filtres;
using SomoniBank.Domain.Models;
using SomoniBank.Infrastructure.Data;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Services;

public class DepositService : IDepositService
{
    private readonly AppDbContext _db;
    private readonly ILogger<DepositService> _logger;

    public DepositService(AppDbContext db, ILogger<DepositService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<DepositGetDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var deposit = await _db.Deposits.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (deposit == null)
                return new Response<DepositGetDto>(HttpStatusCode.NotFound, "Вклад не найден");

            return new Response<DepositGetDto>(HttpStatusCode.OK, "Успешно", MapToDto(deposit));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetDepositById failed");
            return new Response<DepositGetDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<PagedResult<DepositGetDto>> GetAllAsync(DepositFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<Deposit> query = _db.Deposits.AsNoTracking();

        if (filter?.AccountId != null)
            query = query.Where(x => x.AccountId == filter.AccountId);
        if (!string.IsNullOrEmpty(filter?.Status))
            query = query.Where(x => x.Status == Enum.Parse<DepositStatus>(filter.Status, true));
        if (!string.IsNullOrEmpty(filter?.Currency))
            query = query.Where(x => x.Currency == Enum.Parse<Currency>(filter.Currency, true));

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<DepositGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<string>> CreateAsync(Guid userId, DepositInsertDto dto)
    {
        using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<string>(HttpStatusCode.NotFound, "Счёт не найден");

            if (!account.IsActive)
                return new Response<string>(HttpStatusCode.BadRequest, "Счёт заблокирован");

            if (account.Balance < dto.Amount)
                return new Response<string>(HttpStatusCode.BadRequest, "Недостаточно средств");

            // Процентная ставка зависит от срока
            var interestRate = dto.TermMonths switch
            {
                <= 3 => 8.0m,
                <= 6 => 10.0m,
                <= 12 => 12.0m,
                _ => 14.0m
            };

            account.Balance -= dto.Amount;

            var deposit = new Deposit
            {
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                InterestRate = interestRate,
                Currency = Enum.Parse<Currency>(dto.Currency, true),
                Status = DepositStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(dto.TermMonths)
            };

            _db.Deposits.Add(deposit);

            _db.Transactions.Add(new Transaction
            {
                FromAccountId = account.Id,
                Amount = dto.Amount,
                Currency = account.Currency,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                Description = $"Открытие вклада на {dto.TermMonths} месяцев"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return new Response<string>(HttpStatusCode.OK,
                $"Вклад открыт. Ставка: {interestRate}%. Срок: {dto.TermMonths} мес.");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "CreateDeposit failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> CloseAsync(Guid id)
    {
        using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var deposit = await _db.Deposits
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (deposit == null)
                return new Response<string>(HttpStatusCode.NotFound, "Вклад не найден");

            if (deposit.Status != DepositStatus.Active)
                return new Response<string>(HttpStatusCode.BadRequest, "Вклад уже закрыт");

            // Рассчитать прибыль
            var months = (decimal)(DateTime.UtcNow - deposit.StartDate).TotalDays / 30;
            var profit = deposit.Amount * (deposit.InterestRate / 100) * (months / 12);
            var totalReturn = deposit.Amount + profit;

            deposit.Status = DepositStatus.Closed;
            deposit.Account.Balance += totalReturn;

            _db.Transactions.Add(new Transaction
            {
                ToAccountId = deposit.AccountId,
                Amount = totalReturn,
                Currency = deposit.Currency,
                Type = TransactionType.DepositInterest,
                Status = TransactionStatus.Completed,
                Description = $"Закрытие вклада. Прибыль: {profit:F2}"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            return new Response<string>(HttpStatusCode.OK,
                $"Вклад закрыт. Возвращено: {totalReturn:F2}. Прибыль: {profit:F2}");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "CloseDeposit failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    private static DepositGetDto MapToDto(Deposit d) => new()
    {
        Id = d.Id,
        AccountId = d.AccountId,
        Amount = d.Amount,
        InterestRate = d.InterestRate,
        Currency = d.Currency.ToString(),
        Status = d.Status.ToString(),
        StartDate = d.StartDate,
        EndDate = d.EndDate,
        ExpectedProfit = d.Amount * (d.InterestRate / 100) *
            ((decimal)(d.EndDate - d.StartDate).TotalDays / 365)
    };
}