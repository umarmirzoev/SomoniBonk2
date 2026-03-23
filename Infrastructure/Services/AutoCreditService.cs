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

public class AutoCreditService : IAutoCreditService
{
    private const decimal AnnualInterestRate = 15m;
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ICreditScoringService _creditScoringService;
    private readonly ILogger<AutoCreditService> _logger;

    public AutoCreditService(AppDbContext db, INotificationService notificationService, ICreditScoringService creditScoringService, ILogger<AutoCreditService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _creditScoringService = creditScoringService;
        _logger = logger;
    }

    public async Task<Response<AutoCreditGetDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var autoCredit = await _db.AutoCredits.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (autoCredit == null)
                return new Response<AutoCreditGetDto>(HttpStatusCode.NotFound, "Auto credit not found");

            return new Response<AutoCreditGetDto>(HttpStatusCode.OK, "Success", MapToDto(autoCredit));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get auto credit by id failed");
            return new Response<AutoCreditGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<AutoCreditGetDto>> GetAllAsync(AutoCreditFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;
        filter ??= new AutoCreditFilter();

        IQueryable<AutoCredit> query = _db.AutoCredits.AsNoTracking();

        if (filter.UserId.HasValue)
            query = query.Where(x => x.UserId == filter.UserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<LoanStatus>(filter.Status, true, out var status))
            query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Currency) && Enum.TryParse<Currency>(filter.Currency, true, out var currency))
            query = query.Where(x => x.Currency == currency);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<AutoCreditGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<PagedResult<AutoCreditGetDto>> GetMyAsync(Guid userId, PagedQuery pagedQuery)
    {
        return await GetAllAsync(new AutoCreditFilter { UserId = userId }, pagedQuery);
    }

    public async Task<Response<AutoCreditGetDto>> ApplyAsync(Guid userId, AutoCreditInsertDto dto)
    {
        try
        {
            if (dto.CarPrice <= 0 || dto.DownPayment < 0 || dto.TermMonths <= 0)
                return new Response<AutoCreditGetDto>(HttpStatusCode.BadRequest, "Invalid auto credit request");

            if (dto.DownPayment >= dto.CarPrice)
                return new Response<AutoCreditGetDto>(HttpStatusCode.BadRequest, "Down payment must be less than car price");

            if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
                return new Response<AutoCreditGetDto>(HttpStatusCode.BadRequest, "Invalid currency");

            var account = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<AutoCreditGetDto>(HttpStatusCode.NotFound, "Account not found");

            if (!account.IsActive)
                return new Response<AutoCreditGetDto>(HttpStatusCode.BadRequest, "Account is inactive");

            if (account.Currency != currency)
                return new Response<AutoCreditGetDto>(HttpStatusCode.BadRequest, "Auto credit currency must match account currency");

            var loanAmount = dto.CarPrice - dto.DownPayment;
            var scoreDecision = await _creditScoringService.EvaluateApplicationAsync(userId, loanAmount, "Auto credit application");
            if (!scoreDecision.CanProceed)
                return new Response<AutoCreditGetDto>(HttpStatusCode.BadRequest, $"Auto credit application rejected by credit scoring. Score: {scoreDecision.Result.ScoreValue}, decision: {scoreDecision.Result.Decision}");

            var monthlyRate = AnnualInterestRate / 100 / 12;
            var monthlyPayment = loanAmount * monthlyRate / (1 - (decimal)Math.Pow((double)(1 + monthlyRate), -dto.TermMonths));

            var autoCredit = new AutoCredit
            {
                UserId = userId,
                AccountId = dto.AccountId,
                CarBrand = dto.CarBrand,
                CarModel = dto.CarModel,
                CarYear = dto.CarYear,
                CarPrice = dto.CarPrice,
                DownPayment = dto.DownPayment,
                LoanAmount = loanAmount,
                InterestRate = AnnualInterestRate,
                MonthlyPayment = Math.Round(monthlyPayment, 2),
                RemainingAmount = loanAmount,
                TermMonths = dto.TermMonths,
                Currency = currency,
                Status = LoanStatus.Pending,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(dto.TermMonths)
            };

            _db.AutoCredits.Add(autoCredit);
            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "Auto credit application", $"Your auto credit request for {dto.CarBrand} {dto.CarModel} has been created with credit decision {scoreDecision.Result.Decision}.", "AutoCredit");

            return new Response<AutoCreditGetDto>(HttpStatusCode.OK, "Auto credit application created successfully", MapToDto(autoCredit));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Apply auto credit failed");
            return new Response<AutoCreditGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> ApproveAsync(Guid id)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var autoCredit = await _db.AutoCredits.Include(x => x.Account).FirstOrDefaultAsync(x => x.Id == id);
            if (autoCredit == null)
                return new Response<string>(HttpStatusCode.NotFound, "Auto credit not found");

            if (autoCredit.Status != LoanStatus.Pending)
                return new Response<string>(HttpStatusCode.BadRequest, "Auto credit has already been processed");

            var scoreDecision = await _creditScoringService.EvaluateApplicationAsync(autoCredit.UserId, autoCredit.LoanAmount, "Auto credit approval validation");
            if (!scoreDecision.CanProceed)
                return new Response<string>(HttpStatusCode.BadRequest, "Auto credit approval blocked by credit scoring");

            autoCredit.Status = LoanStatus.Active;
            autoCredit.StartDate = DateTime.UtcNow;
            autoCredit.EndDate = autoCredit.StartDate.AddMonths(autoCredit.TermMonths);
            autoCredit.Account.Balance += autoCredit.LoanAmount;

            _db.Transactions.Add(new Transaction
            {
                ToAccountId = autoCredit.AccountId,
                Amount = autoCredit.LoanAmount,
                Currency = autoCredit.Currency,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                Description = $"Auto credit approved for {autoCredit.CarBrand} {autoCredit.CarModel}"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await _notificationService.SendAsync(autoCredit.UserId, "Auto credit approved", $"Your auto credit for {autoCredit.CarBrand} {autoCredit.CarModel} has been approved.", "AutoCredit");

            return new Response<string>(HttpStatusCode.OK, "Auto credit approved successfully");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Approve auto credit failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> RejectAsync(Guid id)
    {
        try
        {
            var autoCredit = await _db.AutoCredits.FirstOrDefaultAsync(x => x.Id == id);
            if (autoCredit == null)
                return new Response<string>(HttpStatusCode.NotFound, "Auto credit not found");

            if (autoCredit.Status != LoanStatus.Pending)
                return new Response<string>(HttpStatusCode.BadRequest, "Auto credit has already been processed");

            autoCredit.Status = LoanStatus.Rejected;
            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(autoCredit.UserId, "Auto credit rejected", $"Your auto credit for {autoCredit.CarBrand} {autoCredit.CarModel} was rejected.", "AutoCredit");

            return new Response<string>(HttpStatusCode.OK, "Auto credit rejected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reject auto credit failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> PayAsync(Guid userId, AutoCreditPaymentDto dto)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var autoCredit = await _db.AutoCredits.FirstOrDefaultAsync(x => x.Id == dto.AutoCreditId && x.UserId == userId);
            if (autoCredit == null)
                return new Response<string>(HttpStatusCode.NotFound, "Auto credit not found");

            if (autoCredit.Status != LoanStatus.Active)
                return new Response<string>(HttpStatusCode.BadRequest, "Auto credit is not active");

            var account = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == dto.FromAccountId && x.UserId == userId);
            if (account == null)
                return new Response<string>(HttpStatusCode.NotFound, "Account not found");

            if (!account.IsActive)
                return new Response<string>(HttpStatusCode.BadRequest, "Account is inactive");

            if (account.Currency != autoCredit.Currency)
                return new Response<string>(HttpStatusCode.BadRequest, "Payment account currency must match auto credit currency");

            var paymentAmount = autoCredit.RemainingAmount < autoCredit.MonthlyPayment ? autoCredit.RemainingAmount : autoCredit.MonthlyPayment;
            if (account.Balance < paymentAmount)
                return new Response<string>(HttpStatusCode.BadRequest, "Insufficient funds");

            account.Balance -= paymentAmount;
            autoCredit.RemainingAmount -= paymentAmount;

            if (autoCredit.RemainingAmount <= 0)
            {
                autoCredit.RemainingAmount = 0;
                autoCredit.Status = LoanStatus.Paid;
            }

            _db.Transactions.Add(new Transaction
            {
                FromAccountId = account.Id,
                Amount = paymentAmount,
                Currency = autoCredit.Currency,
                Type = TransactionType.LoanPayment,
                Status = TransactionStatus.Completed,
                Description = $"Auto credit payment for {autoCredit.CarBrand} {autoCredit.CarModel}"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await _notificationService.SendAsync(userId, "Auto credit payment", $"Payment of {paymentAmount} {autoCredit.Currency} for your auto credit was processed.", "AutoCredit");

            return new Response<string>(HttpStatusCode.OK, $"Auto credit payment completed. Remaining amount: {autoCredit.RemainingAmount}");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Pay auto credit failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private static AutoCreditGetDto MapToDto(AutoCredit autoCredit) => new()
    {
        Id = autoCredit.Id,
        CarBrand = autoCredit.CarBrand,
        CarModel = autoCredit.CarModel,
        CarYear = autoCredit.CarYear,
        CarPrice = autoCredit.CarPrice,
        DownPayment = autoCredit.DownPayment,
        LoanAmount = autoCredit.LoanAmount,
        InterestRate = autoCredit.InterestRate,
        MonthlyPayment = autoCredit.MonthlyPayment,
        RemainingAmount = autoCredit.RemainingAmount,
        TermMonths = autoCredit.TermMonths,
        Currency = autoCredit.Currency.ToString(),
        Status = autoCredit.Status.ToString(),
        StartDate = autoCredit.StartDate,
        EndDate = autoCredit.EndDate
    };
}
