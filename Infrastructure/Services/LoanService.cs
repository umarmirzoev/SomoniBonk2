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

public class LoanService : ILoanService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ICreditScoringService _creditScoringService;
    private readonly ILogger<LoanService> _logger;

    public LoanService(AppDbContext db, INotificationService notificationService, ICreditScoringService creditScoringService, ILogger<LoanService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _creditScoringService = creditScoringService;
        _logger = logger;
    }

    public async Task<Response<LoanGetDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var loan = await _db.Loans.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (loan == null)
                return new Response<LoanGetDto>(HttpStatusCode.NotFound, "Loan not found");

            return new Response<LoanGetDto>(HttpStatusCode.OK, "Success", MapToDto(loan));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetLoanById failed");
            return new Response<LoanGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<LoanGetDto>> GetAllAsync(LoanFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<Loan> query = _db.Loans.AsNoTracking();

        if (filter?.UserId != null)
            query = query.Where(x => x.UserId == filter.UserId);
        if (!string.IsNullOrEmpty(filter?.Status))
            query = query.Where(x => x.Status == Enum.Parse<LoanStatus>(filter.Status, true));
        if (!string.IsNullOrEmpty(filter?.Currency))
            query = query.Where(x => x.Currency == Enum.Parse<Currency>(filter.Currency, true));

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<LoanGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<string>> ApplyAsync(Guid userId, LoanInsertDto dto)
    {
        try
        {
            var account = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<string>(HttpStatusCode.NotFound, "Account not found");

            if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
                return new Response<string>(HttpStatusCode.BadRequest, "Invalid currency");

            var hasActiveLoan = await _db.Loans.AnyAsync(x => x.UserId == userId && x.Status == LoanStatus.Active);
            if (hasActiveLoan)
                return new Response<string>(HttpStatusCode.BadRequest, "You already have an active loan");

            var scoreDecision = await _creditScoringService.EvaluateApplicationAsync(userId, dto.Amount, "Loan application");
            if (!scoreDecision.CanProceed)
                return new Response<string>(HttpStatusCode.BadRequest, $"Loan application rejected by credit scoring. Score: {scoreDecision.Result.ScoreValue}, decision: {scoreDecision.Result.Decision}");

            var interestRate = dto.TermMonths switch
            {
                <= 6 => 18.0m,
                <= 12 => 20.0m,
                <= 24 => 22.0m,
                _ => 24.0m
            };

            var monthlyRate = interestRate / 100 / 12;
            var monthlyPayment = dto.Amount * monthlyRate / (1 - (decimal)Math.Pow((double)(1 + monthlyRate), -dto.TermMonths));

            var loan = new Loan
            {
                UserId = userId,
                AccountId = dto.AccountId,
                Amount = dto.Amount,
                InterestRate = interestRate,
                MonthlyPayment = Math.Round(monthlyPayment, 2),
                RemainingAmount = dto.Amount,
                TermMonths = dto.TermMonths,
                Currency = currency,
                Status = LoanStatus.Pending
            };

            _db.Loans.Add(loan);
            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "Loan application", $"Your loan application was created with credit decision {scoreDecision.Result.Decision}.", "Loan");

            return new Response<string>(HttpStatusCode.OK, $"Loan application created. Monthly payment: {monthlyPayment:F2}. Credit decision: {scoreDecision.Result.Decision}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ApplyLoan failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> ApproveAsync(Guid id)
    {
        using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var loan = await _db.Loans.Include(x => x.Account).FirstOrDefaultAsync(x => x.Id == id);
            if (loan == null)
                return new Response<string>(HttpStatusCode.NotFound, "Loan not found");

            if (loan.Status != LoanStatus.Pending)
                return new Response<string>(HttpStatusCode.BadRequest, "Loan has already been processed");

            var scoreDecision = await _creditScoringService.EvaluateApplicationAsync(loan.UserId, loan.Amount, "Loan approval validation");
            if (!scoreDecision.CanProceed)
                return new Response<string>(HttpStatusCode.BadRequest, "Loan approval blocked by credit scoring");

            loan.Status = LoanStatus.Active;
            loan.StartDate = DateTime.UtcNow;
            loan.EndDate = DateTime.UtcNow.AddMonths(loan.TermMonths);
            loan.Account.Balance += loan.Amount;

            _db.Transactions.Add(new Transaction
            {
                ToAccountId = loan.AccountId,
                Amount = loan.Amount,
                Currency = loan.Currency,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                Description = $"Loan disbursement for {loan.TermMonths} months"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await _notificationService.SendAsync(loan.UserId, "Loan approved", "Your loan application has been approved and funded.", "Loan");

            return new Response<string>(HttpStatusCode.OK, "Loan approved and funded successfully");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "ApproveLoan failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> RejectAsync(Guid id)
    {
        try
        {
            var loan = await _db.Loans.FindAsync(id);
            if (loan == null)
                return new Response<string>(HttpStatusCode.NotFound, "Loan not found");

            if (loan.Status != LoanStatus.Pending)
                return new Response<string>(HttpStatusCode.BadRequest, "Loan has already been processed");

            loan.Status = LoanStatus.Rejected;
            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(loan.UserId, "Loan rejected", "Your loan application was rejected.", "Loan");

            return new Response<string>(HttpStatusCode.OK, "Loan rejected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RejectLoan failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> PayAsync(Guid userId, LoanPaymentDto dto)
    {
        using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var loan = await _db.Loans.FirstOrDefaultAsync(x => x.Id == dto.LoanId && x.UserId == userId);
            if (loan == null)
                return new Response<string>(HttpStatusCode.NotFound, "Loan not found");

            if (loan.Status != LoanStatus.Active)
                return new Response<string>(HttpStatusCode.BadRequest, "Loan is not active");

            var account = await _db.Accounts.FirstOrDefaultAsync(x => x.Id == dto.FromAccountId && x.UserId == userId);
            if (account == null)
                return new Response<string>(HttpStatusCode.NotFound, "Account not found");

            if (account.Balance < dto.Amount)
                return new Response<string>(HttpStatusCode.BadRequest, "Insufficient funds");

            account.Balance -= dto.Amount;
            loan.RemainingAmount -= dto.Amount;

            if (loan.RemainingAmount <= 0)
            {
                loan.RemainingAmount = 0;
                loan.Status = LoanStatus.Paid;
            }

            _db.Transactions.Add(new Transaction
            {
                FromAccountId = account.Id,
                Amount = dto.Amount,
                Currency = loan.Currency,
                Type = TransactionType.LoanPayment,
                Status = TransactionStatus.Completed,
                Description = $"Loan payment. Remaining amount: {loan.RemainingAmount:F2}"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await _notificationService.SendAsync(userId, "Loan payment", $"Loan payment of {dto.Amount} {loan.Currency} completed.", "Loan");

            return new Response<string>(HttpStatusCode.OK, $"Payment accepted. Remaining loan amount: {loan.RemainingAmount:F2}");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "PayLoan failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private static LoanGetDto MapToDto(Loan loan) => new()
    {
        Id = loan.Id,
        AccountId = loan.AccountId,
        Amount = loan.Amount,
        InterestRate = loan.InterestRate,
        MonthlyPayment = loan.MonthlyPayment,
        RemainingAmount = loan.RemainingAmount,
        TermMonths = loan.TermMonths,
        Currency = loan.Currency.ToString(),
        Status = loan.Status.ToString(),
        StartDate = loan.StartDate,
        EndDate = loan.EndDate
    };
}
