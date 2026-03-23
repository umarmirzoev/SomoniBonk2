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

public class InstallmentService : IInstallmentService
{
    private readonly AppDbContext _db;
    private readonly ILogger<InstallmentService> _logger;
    private readonly INotificationService _notification;

    public InstallmentService(AppDbContext db, ILogger<InstallmentService> logger, INotificationService notification)
    {
        _db = db;
        _logger = logger;
        _notification = notification;
    }

    public async Task<Response<InstallmentGetDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var inst = await _db.Installments.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (inst == null)
                return new Response<InstallmentGetDto>(HttpStatusCode.NotFound, "Рассрочка не найдена");

            return new Response<InstallmentGetDto>(HttpStatusCode.OK, "Успешно", MapToDto(inst));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetInstallmentById failed");
            return new Response<InstallmentGetDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<PagedResult<InstallmentGetDto>> GetAllAsync(InstallmentFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<Installment> query = _db.Installments.AsNoTracking();

        if (filter?.UserId != null)
            query = query.Where(x => x.UserId == filter.UserId);
        if (!string.IsNullOrEmpty(filter?.Status))
            query = query.Where(x => x.Status == Enum.Parse<InstallmentStatus>(filter.Status, true));
        if (!string.IsNullOrEmpty(filter?.Currency))
            query = query.Where(x => x.Currency == Enum.Parse<Currency>(filter.Currency, true));

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<InstallmentGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<string>> CreateAsync(Guid userId, InstallmentInsertDto dto)
    {
        try
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<string>(HttpStatusCode.NotFound, "Счёт не найден");

            var monthlyPayment = Math.Round(dto.TotalAmount / dto.TermMonths, 2);

            var installment = new Installment
            {
                UserId = userId,
                AccountId = dto.AccountId,
                ProductName = dto.ProductName,
                TotalAmount = dto.TotalAmount,
                MonthlyPayment = monthlyPayment,
                RemainingAmount = dto.TotalAmount,
                TermMonths = dto.TermMonths,
                Currency = Enum.Parse<Currency>(dto.Currency, true),
                NextPaymentDate = DateTime.UtcNow.AddMonths(1)
            };

            _db.Installments.Add(installment);
            account.Balance += dto.TotalAmount;

            _db.Transactions.Add(new Transaction
            {
                ToAccountId = account.Id,
                Amount = dto.TotalAmount,
                Currency = account.Currency,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                Description = $"Рассрочка: {dto.ProductName}"
            });

            await _db.SaveChangesAsync();

            await _notification.SendAsync(userId, "Рассрочка оформлена",
                $"{dto.ProductName} — {dto.TotalAmount} {dto.Currency} на {dto.TermMonths} мес. Платёж: {monthlyPayment}/мес.",
                "installment");

            return new Response<string>(HttpStatusCode.OK,
                $"Рассрочка оформлена. Ежемесячный платёж: {monthlyPayment} {dto.Currency}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateInstallment failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> PayAsync(Guid userId, InstallmentPaymentDto dto)
    {
        using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var installment = await _db.Installments
                .FirstOrDefaultAsync(x => x.Id == dto.InstallmentId && x.UserId == userId);
            if (installment == null)
                return new Response<string>(HttpStatusCode.NotFound, "Рассрочка не найдена");

            if (installment.Status != InstallmentStatus.Active)
                return new Response<string>(HttpStatusCode.BadRequest, "Рассрочка не активна");

            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.FromAccountId && x.UserId == userId);
            if (account == null)
                return new Response<string>(HttpStatusCode.NotFound, "Счёт не найден");

            if (account.Balance < installment.MonthlyPayment)
                return new Response<string>(HttpStatusCode.BadRequest, "Недостаточно средств");

            account.Balance -= installment.MonthlyPayment;
            installment.PaidAmount += installment.MonthlyPayment;
            installment.RemainingAmount -= installment.MonthlyPayment;
            installment.PaidMonths++;
            installment.NextPaymentDate = DateTime.UtcNow.AddMonths(1);

            if (installment.PaidMonths >= installment.TermMonths)
            {
                installment.Status = InstallmentStatus.Paid;
                installment.RemainingAmount = 0;
            }

            _db.Transactions.Add(new Transaction
            {
                FromAccountId = account.Id,
                Amount = installment.MonthlyPayment,
                Currency = installment.Currency,
                Type = TransactionType.LoanPayment,
                Status = TransactionStatus.Completed,
                Description = $"Платёж по рассрочке: {installment.ProductName}"
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();

            await _notification.SendAsync(userId, "Платёж по рассрочке",
                $"Принят платёж {installment.MonthlyPayment} {installment.Currency}. Остаток: {installment.RemainingAmount}",
                "payment");

            return new Response<string>(HttpStatusCode.OK,
                $"Платёж принят. Остаток: {installment.RemainingAmount} {installment.Currency}");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "PayInstallment failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> CancelAsync(Guid id)
    {
        try
        {
            var installment = await _db.Installments.FindAsync(id);
            if (installment == null)
                return new Response<string>(HttpStatusCode.NotFound, "Рассрочка не найдена");

            installment.Status = InstallmentStatus.Cancelled;
            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Рассрочка отменена");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelInstallment failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    private static InstallmentGetDto MapToDto(Installment i) => new()
    {
        Id = i.Id,
        ProductName = i.ProductName,
        TotalAmount = i.TotalAmount,
        MonthlyPayment = i.MonthlyPayment,
        PaidAmount = i.PaidAmount,
        RemainingAmount = i.RemainingAmount,
        TermMonths = i.TermMonths,
        PaidMonths = i.PaidMonths,
        Currency = i.Currency.ToString(),
        Status = i.Status.ToString(),
        NextPaymentDate = i.NextPaymentDate
    };
}