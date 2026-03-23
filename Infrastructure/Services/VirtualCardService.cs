using System.Net;
using System.Security.Cryptography;
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

public class VirtualCardService : IVirtualCardService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IFraudDetectionService _fraudDetectionService;
    private readonly ILogger<VirtualCardService> _logger;

    public VirtualCardService(
        AppDbContext db,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        IFraudDetectionService fraudDetectionService,
        ILogger<VirtualCardService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _fraudDetectionService = fraudDetectionService;
        _logger = logger;
    }

    public async Task<Response<VirtualCardCreateResultDto>> CreateAsync(Guid userId, VirtualCardInsertDto dto)
    {
        try
        {
            if (dto.DailyLimit <= 0 || dto.MonthlyLimit <= 0 || dto.MonthlyLimit < dto.DailyLimit)
                return new Response<VirtualCardCreateResultDto>(HttpStatusCode.BadRequest, "Invalid card limits");

            var account = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == dto.LinkedAccountId && x.UserId == userId);
            if (account == null)
                return new Response<VirtualCardCreateResultDto>(HttpStatusCode.NotFound, "Account not found");

            if (!account.IsActive)
                return new Response<VirtualCardCreateResultDto>(HttpStatusCode.BadRequest, "Account is inactive");

            var cardNumber = GenerateCardNumber();
            var cvv = GenerateThreeDigits();
            var virtualCard = new VirtualCard
            {
                UserId = userId,
                LinkedAccountId = account.Id,
                CardHolderName = dto.CardHolderName.Trim().ToUpperInvariant(),
                MaskedCardNumber = $"**** **** **** {cardNumber[^4..]}",
                ExpiryMonth = DateTime.UtcNow.AddYears(3).Month,
                ExpiryYear = DateTime.UtcNow.AddYears(3).Year,
                CvvHash = BCrypt.Net.BCrypt.HashPassword(cvv),
                DailyLimit = dto.DailyLimit,
                MonthlyLimit = dto.MonthlyLimit,
                IsSingleUse = dto.IsSingleUse
            };

            _db.VirtualCards.Add(virtualCard);
            await _db.SaveChangesAsync();

            await _notificationService.SendAsync(userId, "Virtual card created", $"Your virtual card {virtualCard.MaskedCardNumber} has been created.", "VirtualCard");
            await _auditLogService.LogAsync(userId, "VirtualCard:Create", "system", "virtual-card", true);

            return new Response<VirtualCardCreateResultDto>(HttpStatusCode.OK, "Virtual card created successfully", new VirtualCardCreateResultDto
            {
                Card = MapToDto(virtualCard),
                GeneratedCvv = cvv
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create virtual card failed");
            return new Response<VirtualCardCreateResultDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<VirtualCardGetDto>> GetMyAsync(Guid userId, VirtualCardFilter filter, PagedQuery pagedQuery)
    {
        filter ??= new VirtualCardFilter();
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<VirtualCard> query = _db.VirtualCards.AsNoTracking().Where(x => x.UserId == userId);

        if (filter.LinkedAccountId.HasValue)
            query = query.Where(x => x.LinkedAccountId == filter.LinkedAccountId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<VirtualCardStatus>(filter.Status, true, out var status))
            query = query.Where(x => x.Status == status);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<VirtualCardGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<VirtualCardGetDto>> GetByIdAsync(Guid userId, Guid id)
    {
        try
        {
            var card = await _db.VirtualCards.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (card == null)
                return new Response<VirtualCardGetDto>(HttpStatusCode.NotFound, "Virtual card not found");

            return new Response<VirtualCardGetDto>(HttpStatusCode.OK, "Success", MapToDto(card));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get virtual card by id failed");
            return new Response<VirtualCardGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public Task<Response<string>> FreezeAsync(Guid userId, Guid id)
        => UpdateStatusAsync(userId, id, VirtualCardStatus.Frozen, "VirtualCard:Freeze", "Virtual card frozen");

    public Task<Response<string>> UnfreezeAsync(Guid userId, Guid id)
        => UpdateStatusAsync(userId, id, VirtualCardStatus.Active, "VirtualCard:Unfreeze", "Virtual card reactivated");

    public Task<Response<string>> CancelAsync(Guid userId, Guid id)
        => UpdateStatusAsync(userId, id, VirtualCardStatus.Cancelled, "VirtualCard:Cancel", "Virtual card cancelled");

    public async Task<Response<string>> UpdateLimitsAsync(Guid userId, Guid id, VirtualCardLimitUpdateDto dto)
    {
        try
        {
            if (dto.DailyLimit <= 0 || dto.MonthlyLimit <= 0 || dto.MonthlyLimit < dto.DailyLimit)
                return new Response<string>(HttpStatusCode.BadRequest, "Invalid card limits");

            var card = await _db.VirtualCards.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (card == null)
                return new Response<string>(HttpStatusCode.NotFound, "Virtual card not found");

            card.DailyLimit = dto.DailyLimit;
            card.MonthlyLimit = dto.MonthlyLimit;
            await _db.SaveChangesAsync();

            await _notificationService.SendAsync(userId, "Virtual card limits updated", $"Limits were updated for card {card.MaskedCardNumber}.", "VirtualCard");
            await _auditLogService.LogAsync(userId, "VirtualCard:UpdateLimits", "system", "virtual-card", true);

            return new Response<string>(HttpStatusCode.OK, "Virtual card limits updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update virtual card limits failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<VirtualCardPaymentResultDto>> UseForPaymentAsync(Guid userId, VirtualCardPaymentDto dto)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (dto.Amount <= 0)
                return new Response<VirtualCardPaymentResultDto>(HttpStatusCode.BadRequest, "Amount must be greater than zero");

            var card = await _db.VirtualCards
                .Include(x => x.LinkedAccount)
                .FirstOrDefaultAsync(x => x.Id == dto.VirtualCardId && x.UserId == userId);
            if (card == null)
                return new Response<VirtualCardPaymentResultDto>(HttpStatusCode.NotFound, "Virtual card not found");

            if (IsExpired(card))
            {
                card.Status = VirtualCardStatus.Expired;
                await _db.SaveChangesAsync();
                return new Response<VirtualCardPaymentResultDto>(HttpStatusCode.BadRequest, "Virtual card expired");
            }

            if (card.Status != VirtualCardStatus.Active)
            {
                await _fraudDetectionService.ReportBlockedCardUsageAsync(userId, card.MaskedCardNumber, "virtual-card-payment", $"Status: {card.Status}");
                return new Response<VirtualCardPaymentResultDto>(HttpStatusCode.BadRequest, $"Virtual card is {card.Status}");
            }

            if (!card.LinkedAccount.IsActive)
                return new Response<VirtualCardPaymentResultDto>(HttpStatusCode.BadRequest, "Linked account is inactive");

            if (card.LinkedAccount.Balance < dto.Amount)
                return new Response<VirtualCardPaymentResultDto>(HttpStatusCode.BadRequest, "Insufficient funds");

            var dayStart = DateTime.UtcNow.Date;
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            var spentToday = await _db.Transactions.AsNoTracking()
                .Where(x => x.VirtualCardId == card.Id
                            && x.Status == TransactionStatus.Completed
                            && x.CreatedAt >= dayStart)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            var spentThisMonth = await _db.Transactions.AsNoTracking()
                .Where(x => x.VirtualCardId == card.Id
                            && x.Status == TransactionStatus.Completed
                            && x.CreatedAt >= monthStart)
                .SumAsync(x => (decimal?)x.Amount) ?? 0m;

            if (spentToday + dto.Amount > card.DailyLimit)
                return new Response<VirtualCardPaymentResultDto>(HttpStatusCode.BadRequest, "Daily limit exceeded");

            if (spentThisMonth + dto.Amount > card.MonthlyLimit)
                return new Response<VirtualCardPaymentResultDto>(HttpStatusCode.BadRequest, "Monthly limit exceeded");

            card.LinkedAccount.Balance -= dto.Amount;

            var transaction = new Transaction
            {
                FromAccountId = card.LinkedAccountId,
                VirtualCardId = card.Id,
                Amount = dto.Amount,
                Currency = card.LinkedAccount.Currency,
                Type = TransactionType.Withdrawal,
                Status = TransactionStatus.Completed,
                Description = $"Virtual card payment to {dto.MerchantName}"
            };

            _db.Transactions.Add(transaction);

            if (card.IsSingleUse)
                card.Status = VirtualCardStatus.Cancelled;

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await _notificationService.SendAsync(userId, "Virtual card payment", $"Payment of {dto.Amount} {card.LinkedAccount.Currency} to {dto.MerchantName} completed.", "VirtualCard");
            await _auditLogService.LogAsync(userId, "VirtualCard:Payment", "system", dto.MerchantName, true);

            return new Response<VirtualCardPaymentResultDto>(HttpStatusCode.OK, "Virtual card payment completed", new VirtualCardPaymentResultDto
            {
                TransactionId = transaction.Id,
                Amount = transaction.Amount,
                Currency = transaction.Currency.ToString(),
                Status = transaction.Status.ToString(),
                Description = transaction.Description
            });
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Use virtual card for payment failed");
            return new Response<VirtualCardPaymentResultDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private async Task<Response<string>> UpdateStatusAsync(Guid userId, Guid id, VirtualCardStatus status, string action, string successMessage)
    {
        try
        {
            var card = await _db.VirtualCards.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (card == null)
                return new Response<string>(HttpStatusCode.NotFound, "Virtual card not found");

            card.Status = status;
            await _db.SaveChangesAsync();

            await _notificationService.SendAsync(userId, "Virtual card status updated", $"{card.MaskedCardNumber} is now {status}.", "VirtualCard");
            await _auditLogService.LogAsync(userId, action, "system", "virtual-card", true);

            return new Response<string>(HttpStatusCode.OK, successMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update virtual card status failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private static bool IsExpired(VirtualCard card)
        => card.ExpiryYear < DateTime.UtcNow.Year
           || (card.ExpiryYear == DateTime.UtcNow.Year && card.ExpiryMonth < DateTime.UtcNow.Month);

    private static string GenerateCardNumber()
    {
        var bytes = RandomNumberGenerator.GetBytes(12);
        var digits = string.Concat(bytes.Select(x => (x % 10).ToString()));
        return "4987" + digits[..12];
    }

    private static string GenerateThreeDigits()
    {
        var bytes = RandomNumberGenerator.GetBytes(2);
        var value = (bytes[0] << 8 | bytes[1]) % 900 + 100;
        return value.ToString();
    }

    private static VirtualCardGetDto MapToDto(VirtualCard card) => new()
    {
        Id = card.Id,
        LinkedAccountId = card.LinkedAccountId,
        CardHolderName = card.CardHolderName,
        MaskedCardNumber = card.MaskedCardNumber,
        ExpiryMonth = card.ExpiryMonth,
        ExpiryYear = card.ExpiryYear,
        DailyLimit = card.DailyLimit,
        MonthlyLimit = card.MonthlyLimit,
        IsSingleUse = card.IsSingleUse,
        Status = card.Status.ToString(),
        CreatedAt = card.CreatedAt
    };
}
