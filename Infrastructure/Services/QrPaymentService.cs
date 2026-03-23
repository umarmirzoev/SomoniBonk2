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

public class QrPaymentService : IQrPaymentService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ILogger<QrPaymentService> _logger;

    public QrPaymentService(AppDbContext db, INotificationService notificationService, ILogger<QrPaymentService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Response<QrPaymentGetDto>> GenerateQrAsync(Guid userId, GenerateQrDto dto)
    {
        try
        {
            if (dto.Amount <= 0)
                return new Response<QrPaymentGetDto>(HttpStatusCode.BadRequest, "Amount must be greater than zero");

            if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
                return new Response<QrPaymentGetDto>(HttpStatusCode.BadRequest, "Invalid currency");

            var expiresInMinutes = dto.ExpiresInMinutes <= 0 ? 15 : dto.ExpiresInMinutes;
            var account = await _db.Accounts.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == dto.ToAccountId && x.UserId == userId);
            if (account == null)
                return new Response<QrPaymentGetDto>(HttpStatusCode.NotFound, "Target account not found");

            if (!account.IsActive)
                return new Response<QrPaymentGetDto>(HttpStatusCode.BadRequest, "Target account is inactive");

            if (account.Currency != currency)
                return new Response<QrPaymentGetDto>(HttpStatusCode.BadRequest, "QR currency must match target account currency");

            var qrPayment = new QrPayment
            {
                ToAccountId = account.Id,
                Amount = dto.Amount,
                Currency = currency,
                QrCode = $"SBQR-{Guid.NewGuid():N}",
                Status = QrPaymentStatus.Pending,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expiresInMinutes)
            };

            _db.QrPayments.Add(qrPayment);
            await _db.SaveChangesAsync();

            return new Response<QrPaymentGetDto>(HttpStatusCode.OK, "QR payment generated successfully", MapToDto(qrPayment, account.AccountNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generate QR payment failed");
            return new Response<QrPaymentGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<QrPaymentGetDto>> PayByQrAsync(Guid userId, PayByQrDto dto)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var qrPayment = await _db.QrPayments
                .Include(x => x.ToAccount)
                .FirstOrDefaultAsync(x => x.QrCode == dto.QrCode);
            if (qrPayment == null)
                return new Response<QrPaymentGetDto>(HttpStatusCode.NotFound, "QR payment not found");

            if (qrPayment.Status == QrPaymentStatus.Completed)
                return new Response<QrPaymentGetDto>(HttpStatusCode.BadRequest, "QR payment has already been paid");

            if (qrPayment.ExpiresAt <= DateTime.UtcNow)
            {
                qrPayment.Status = QrPaymentStatus.Expired;
                await _db.SaveChangesAsync();
                await dbTransaction.CommitAsync();
                return new Response<QrPaymentGetDto>(HttpStatusCode.BadRequest, "QR payment has expired");
            }

            var fromAccount = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.FromAccountId && x.UserId == userId);
            if (fromAccount == null)
                return new Response<QrPaymentGetDto>(HttpStatusCode.NotFound, "Source account not found");

            if (!fromAccount.IsActive)
                return new Response<QrPaymentGetDto>(HttpStatusCode.BadRequest, "Source account is inactive");

            if (!qrPayment.ToAccount.IsActive)
                return new Response<QrPaymentGetDto>(HttpStatusCode.BadRequest, "Recipient account is inactive");

            if (fromAccount.Currency != qrPayment.Currency)
                return new Response<QrPaymentGetDto>(HttpStatusCode.BadRequest, "Source account currency must match QR currency");

            if (fromAccount.Balance < qrPayment.Amount)
                return new Response<QrPaymentGetDto>(HttpStatusCode.BadRequest, "Insufficient funds");

            fromAccount.Balance -= qrPayment.Amount;
            qrPayment.ToAccount.Balance += qrPayment.Amount;
            qrPayment.Status = QrPaymentStatus.Completed;
            qrPayment.FromUserId = userId;

            _db.Transactions.Add(new Transaction
            {
                FromAccountId = fromAccount.Id,
                ToAccountId = qrPayment.ToAccountId,
                Amount = qrPayment.Amount,
                Currency = qrPayment.Currency,
                Type = TransactionType.Transfer,
                Status = TransactionStatus.Completed,
                Description = $"QR payment {qrPayment.QrCode}"
            });

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "QR payment", $"QR payment of {qrPayment.Amount} {qrPayment.Currency} completed successfully.", "QrPayment");
            await _notificationService.SendAsync(qrPayment.ToAccount.UserId, "Incoming QR payment", $"You received {qrPayment.Amount} {qrPayment.Currency} via QR payment.", "QrPayment");
            await dbTransaction.CommitAsync();

            return new Response<QrPaymentGetDto>(HttpStatusCode.OK, "QR payment completed successfully", MapToDto(qrPayment, qrPayment.ToAccount.AccountNumber));
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Pay by QR failed");
            return new Response<QrPaymentGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<QrPaymentGetDto>> GetQrStatusAsync(string qrCode)
    {
        try
        {
            var qrPayment = await _db.QrPayments.AsNoTracking()
                .Include(x => x.ToAccount)
                .FirstOrDefaultAsync(x => x.QrCode == qrCode);
            if (qrPayment == null)
                return new Response<QrPaymentGetDto>(HttpStatusCode.NotFound, "QR payment not found");

            var status = qrPayment.ExpiresAt <= DateTime.UtcNow && qrPayment.Status == QrPaymentStatus.Pending
                ? QrPaymentStatus.Expired
                : qrPayment.Status;

            qrPayment.Status = status;
            return new Response<QrPaymentGetDto>(HttpStatusCode.OK, "Success", MapToDto(qrPayment, qrPayment.ToAccount.AccountNumber));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get QR status failed");
            return new Response<QrPaymentGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private static QrPaymentGetDto MapToDto(QrPayment qrPayment, string accountNumber) => new()
    {
        Id = qrPayment.Id,
        FromUserId = qrPayment.FromUserId,
        ToAccountId = qrPayment.ToAccountId,
        ToAccountNumber = accountNumber,
        Amount = qrPayment.Amount,
        Currency = qrPayment.Currency.ToString(),
        QrCode = qrPayment.QrCode,
        Status = qrPayment.Status.ToString(),
        ExpiresAt = qrPayment.ExpiresAt,
        CreatedAt = qrPayment.CreatedAt
    };
}
