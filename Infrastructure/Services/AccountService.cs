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

public class AccountService : IAccountService
{
    private readonly AppDbContext _db;
    private readonly ILogger<AccountService> _logger;

    public AccountService(AppDbContext db, ILogger<AccountService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<AccountGetDto>> GetByIdAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false)
    {
        try
        {
            IQueryable<Account> query = _db.Accounts.AsNoTracking();
            if (!isAdmin && requesterUserId.HasValue)
                query = query.Where(x => x.UserId == requesterUserId.Value);

            var account = await query
                .FirstOrDefaultAsync(x => x.Id == id);
            if (account == null)
                return new Response<AccountGetDto>(HttpStatusCode.NotFound, "Счёт не найден");

            return new Response<AccountGetDto>(HttpStatusCode.OK, "Успешно", MapToDto(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAccountById failed");
            return new Response<AccountGetDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<PagedResult<AccountGetDto>> GetAllAsync(AccountFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<Account> query = _db.Accounts.AsNoTracking();

        if (filter?.UserId != null)
            query = query.Where(x => x.UserId == filter.UserId);
        if (!string.IsNullOrEmpty(filter?.Type))
            query = query.Where(x => x.Type == Enum.Parse<AccountType>(filter.Type, true));
        if (!string.IsNullOrEmpty(filter?.Currency))
            query = query.Where(x => x.Currency == Enum.Parse<Currency>(filter.Currency, true));
        if (filter?.IsActive != null)
            query = query.Where(x => x.IsActive == filter.IsActive);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<AccountGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<AccountGetDto>> CreateAsync(Guid userId, AccountInsertDto dto)
    {
        try
        {
            var user = await _db.Users.FindAsync(userId);
            if (user == null)
                return new Response<AccountGetDto>(HttpStatusCode.NotFound, "Пользователь не найден");

            var account = new Account
            {
                UserId = userId,
                AccountNumber = GenerateAccountNumber(),
                Type = Enum.Parse<AccountType>(dto.Type, true),
                Currency = Enum.Parse<Currency>(dto.Currency, true),
                Balance = 0
            };

            _db.Accounts.Add(account);
            await _db.SaveChangesAsync();

            return new Response<AccountGetDto>(HttpStatusCode.OK, "Счёт успешно создан", MapToDto(account));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAccount failed");
            return new Response<AccountGetDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> CloseAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false)
    {
        try
        {
            IQueryable<Account> query = _db.Accounts;
            if (!isAdmin && requesterUserId.HasValue)
                query = query.Where(x => x.UserId == requesterUserId.Value);

            var account = await query.FirstOrDefaultAsync(x => x.Id == id);
            if (account == null)
                return new Response<string>(HttpStatusCode.NotFound, "Счёт не найден");

            if (account.Balance > 0)
                return new Response<string>(HttpStatusCode.BadRequest, "Нельзя закрыть счёт с остатком");

            account.IsActive = false;
            await _db.SaveChangesAsync();

            return new Response<string>(HttpStatusCode.OK, "Счёт закрыт");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CloseAccount failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<decimal>> GetBalanceAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false)
    {
        try
        {
            IQueryable<Account> query = _db.Accounts.AsNoTracking();
            if (!isAdmin && requesterUserId.HasValue)
                query = query.Where(x => x.UserId == requesterUserId.Value);

            var account = await query
                .FirstOrDefaultAsync(x => x.Id == id);
            if (account == null)
                return new Response<decimal>(HttpStatusCode.NotFound, "Счёт не найден");

            return new Response<decimal>(HttpStatusCode.OK, "Успешно", account.Balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBalance failed");
            return new Response<decimal>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    private static string GenerateAccountNumber()
    {
        var random = new Random();
        return $"TJ{random.Next(10000000, 99999999)}{random.Next(10000000, 99999999)}";
    }

    private static AccountGetDto MapToDto(Account a) => new()
    {
        Id = a.Id,
        AccountNumber = a.AccountNumber,
        Type = a.Type.ToString(),
        Currency = a.Currency.ToString(),
        Balance = a.Balance,
        IsActive = a.IsActive,
        CreatedAt = a.CreatedAt
    };
}
