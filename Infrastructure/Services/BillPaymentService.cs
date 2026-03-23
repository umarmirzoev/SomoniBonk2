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

public class BillPaymentService : IBillPaymentService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly ILogger<BillPaymentService> _logger;

    public BillPaymentService(AppDbContext db, INotificationService notificationService, IFraudDetectionService fraudDetectionService, ILogger<BillPaymentService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _fraudDetectionService = fraudDetectionService;
        _logger = logger;
    }

    public async Task<Response<List<BillCategoryGetDto>>> GetCategoriesAsync()
    {
        try
        {
            var categories = await _db.BillCategories.AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new BillCategoryGetDto
                {
                    Id = x.Id,
                    Name = x.Name,
                    Code = x.Code
                })
                .ToListAsync();

            return new Response<List<BillCategoryGetDto>>(HttpStatusCode.OK, "Success", categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get bill categories failed");
            return new Response<List<BillCategoryGetDto>>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<List<BillProviderGetDto>>> GetProvidersByCategoryAsync(Guid categoryId)
    {
        try
        {
            var categoryExists = await _db.BillCategories.AsNoTracking()
                .AnyAsync(x => x.Id == categoryId && x.IsActive);
            if (!categoryExists)
                return new Response<List<BillProviderGetDto>>(HttpStatusCode.NotFound, "Category not found");

            var providers = await _db.BillProviders.AsNoTracking()
                .Where(x => x.CategoryId == categoryId && x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new BillProviderGetDto
                {
                    Id = x.Id,
                    CategoryId = x.CategoryId,
                    Name = x.Name,
                    Code = x.Code,
                    LogoUrl = x.LogoUrl
                })
                .ToListAsync();

            return new Response<List<BillProviderGetDto>>(HttpStatusCode.OK, "Success", providers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get bill providers failed");
            return new Response<List<BillProviderGetDto>>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<BillPaymentGetDto>> PayBillAsync(Guid userId, BillPaymentInsertDto dto)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (dto.Amount <= 0)
                return new Response<BillPaymentGetDto>(HttpStatusCode.BadRequest, "Amount must be greater than zero");

            if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
                return new Response<BillPaymentGetDto>(HttpStatusCode.BadRequest, "Invalid currency");

            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<BillPaymentGetDto>(HttpStatusCode.NotFound, "Account not found");

            if (!account.IsActive)
                return new Response<BillPaymentGetDto>(HttpStatusCode.BadRequest, "Account is inactive");

            if (account.Currency != currency)
                return new Response<BillPaymentGetDto>(HttpStatusCode.BadRequest, "Payment currency must match account currency");

            if (account.Balance < dto.Amount)
                return new Response<BillPaymentGetDto>(HttpStatusCode.BadRequest, "Insufficient funds");

            var provider = await _db.BillProviders
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == dto.ProviderId && x.IsActive && x.Category.IsActive);
            if (provider == null)
                return new Response<BillPaymentGetDto>(HttpStatusCode.NotFound, "Provider not found");

            var fraudCheck = await _fraudDetectionService.EvaluateBillPaymentAsync(userId, account, dto.Amount, provider.Name);
            if (fraudCheck.IsBlocked)
                return new Response<BillPaymentGetDto>(HttpStatusCode.Forbidden, $"Bill payment blocked by fraud monitoring: {fraudCheck.Reason}");

            account.Balance -= dto.Amount;

            var payment = new BillPayment
            {
                UserId = userId,
                AccountId = account.Id,
                ProviderId = provider.Id,
                AccountNumber = dto.AccountNumber,
                Amount = dto.Amount,
                Currency = currency,
                Status = BillPaymentStatus.Completed,
                Description = dto.Description
            };

            _db.BillPayments.Add(payment);
            _db.Transactions.Add(new Transaction
            {
                FromAccountId = account.Id,
                Amount = dto.Amount,
                Currency = currency,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Completed,
                Description = $"Bill payment to {provider.Name}"
            });

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "Bill payment", $"Payment to {provider.Name} for {dto.Amount} {currency} completed successfully.", "BillPayment");
            await dbTransaction.CommitAsync();

            return new Response<BillPaymentGetDto>(HttpStatusCode.OK, "Bill paid successfully", MapToDto(payment, provider.Name, provider.Category.Name));
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Bill payment failed");
            return new Response<BillPaymentGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<BillPaymentGetDto>> GetMyPaymentsAsync(Guid userId, BillPaymentFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        filter ??= new BillPaymentFilter();

        IQueryable<BillPayment> query = _db.BillPayments.AsNoTracking()
            .Include(x => x.Provider)
            .ThenInclude(x => x.Category)
            .Where(x => x.UserId == userId);

        if (filter.ProviderId.HasValue)
            query = query.Where(x => x.ProviderId == filter.ProviderId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<BillPaymentStatus>(filter.Status, true, out var status))
            query = query.Where(x => x.Status == status);
        if (filter.FromDate.HasValue)
            query = query.Where(x => x.CreatedAt >= filter.FromDate.Value);
        if (filter.ToDate.HasValue)
            query = query.Where(x => x.CreatedAt <= filter.ToDate.Value);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<BillPaymentGetDto>
        {
            Items = items.Select(x => MapToDto(x, x.Provider.Name, x.Provider.Category.Name)).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    private static BillPaymentGetDto MapToDto(BillPayment payment, string providerName, string categoryName) => new()
    {
        Id = payment.Id,
        UserId = payment.UserId,
        AccountId = payment.AccountId,
        ProviderId = payment.ProviderId,
        ProviderName = providerName,
        CategoryName = categoryName,
        AccountNumber = payment.AccountNumber,
        Amount = payment.Amount,
        Currency = payment.Currency.ToString(),
        Status = payment.Status.ToString(),
        Description = payment.Description,
        CreatedAt = payment.CreatedAt
    };
}
