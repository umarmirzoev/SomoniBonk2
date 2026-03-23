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

public class FlightBookingService : IFlightBookingService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly ILogger<FlightBookingService> _logger;

    public FlightBookingService(AppDbContext db, INotificationService notificationService, ILogger<FlightBookingService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Response<FlightBookingGetDto>> BookAsync(Guid userId, FlightBookingInsertDto dto)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (dto.TotalPrice <= 0 || dto.PassengerCount <= 0)
                return new Response<FlightBookingGetDto>(HttpStatusCode.BadRequest, "Invalid flight booking request");

            if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
                return new Response<FlightBookingGetDto>(HttpStatusCode.BadRequest, "Invalid currency");

            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<FlightBookingGetDto>(HttpStatusCode.NotFound, "Account not found");

            if (!account.IsActive)
                return new Response<FlightBookingGetDto>(HttpStatusCode.BadRequest, "Account is inactive");

            if (account.Currency != currency)
                return new Response<FlightBookingGetDto>(HttpStatusCode.BadRequest, "Flight booking currency must match account currency");

            if (dto.DepartureDate <= DateTime.UtcNow)
                return new Response<FlightBookingGetDto>(HttpStatusCode.BadRequest, "Departure date must be in the future");

            if (dto.ReturnDate.HasValue && dto.ReturnDate.Value < dto.DepartureDate)
                return new Response<FlightBookingGetDto>(HttpStatusCode.BadRequest, "Return date must be after departure date");

            var booking = new FlightBooking
            {
                UserId = userId,
                AccountId = dto.AccountId,
                FlightNumber = dto.FlightNumber,
                FromCity = dto.FromCity,
                ToCity = dto.ToCity,
                DepartureDate = dto.DepartureDate,
                ReturnDate = dto.ReturnDate,
                PassengerCount = dto.PassengerCount,
                TotalPrice = dto.TotalPrice,
                IsInstallment = dto.IsInstallment,
                InstallmentMonths = dto.IsInstallment ? dto.InstallmentMonths : null,
                MonthlyPayment = null,
                Currency = currency,
                Status = "Confirmed"
            };

            _db.FlightBookings.Add(booking);

            if (!dto.IsInstallment)
            {
                if (account.Balance < dto.TotalPrice)
                    return new Response<FlightBookingGetDto>(HttpStatusCode.BadRequest, "Insufficient funds");

                account.Balance -= dto.TotalPrice;
                _db.Transactions.Add(new Transaction
                {
                    FromAccountId = account.Id,
                    Amount = dto.TotalPrice,
                    Currency = currency,
                    Type = TransactionType.Withdrawal,
                    Status = TransactionStatus.Completed,
                    Description = $"Flight booking {dto.FlightNumber}"
                });
            }
            else
            {
                if (!dto.InstallmentMonths.HasValue || dto.InstallmentMonths.Value <= 0)
                    return new Response<FlightBookingGetDto>(HttpStatusCode.BadRequest, "Installment months must be greater than zero");

                var monthlyPayment = Math.Round(dto.TotalPrice / dto.InstallmentMonths.Value, 2);
                booking.MonthlyPayment = monthlyPayment;

                await _db.SaveChangesAsync();

                _db.Installments.Add(new Installment
                {
                    UserId = userId,
                    AccountId = account.Id,
                    ProductName = $"Flight booking #{booking.Id}",
                    TotalAmount = dto.TotalPrice,
                    MonthlyPayment = monthlyPayment,
                    RemainingAmount = dto.TotalPrice,
                    TermMonths = dto.InstallmentMonths.Value,
                    Currency = currency,
                    NextPaymentDate = DateTime.UtcNow.AddMonths(1)
                });
            }

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await _notificationService.SendAsync(userId, "Flight booking", $"Flight {dto.FlightNumber} from {dto.FromCity} to {dto.ToCity} has been booked successfully.", "FlightBooking");

            return new Response<FlightBookingGetDto>(HttpStatusCode.OK, "Flight booked successfully", MapToDto(booking));
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Book flight failed");
            return new Response<FlightBookingGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<FlightBookingGetDto>> GetMyBookingsAsync(Guid userId, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var query = _db.FlightBookings.AsNoTracking()
            .Where(x => x.UserId == userId);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<FlightBookingGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<FlightBookingGetDto>> GetByIdAsync(Guid id)
    {
        try
        {
            var booking = await _db.FlightBookings.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (booking == null)
                return new Response<FlightBookingGetDto>(HttpStatusCode.NotFound, "Flight booking not found");

            return new Response<FlightBookingGetDto>(HttpStatusCode.OK, "Success", MapToDto(booking));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get flight booking by id failed");
            return new Response<FlightBookingGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> CancelAsync(Guid userId, Guid id)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var booking = await _db.FlightBookings
                .Include(x => x.Account)
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (booking == null)
                return new Response<string>(HttpStatusCode.NotFound, "Flight booking not found");

            if (booking.Status == "Cancelled")
                return new Response<string>(HttpStatusCode.BadRequest, "Flight booking is already cancelled");

            booking.Status = "Cancelled";
            decimal refundAmount;

            if (!booking.IsInstallment)
            {
                refundAmount = booking.TotalPrice;
                booking.Account.Balance += refundAmount;

                _db.Transactions.Add(new Transaction
                {
                    ToAccountId = booking.AccountId,
                    Amount = refundAmount,
                    Currency = booking.Currency,
                    Type = TransactionType.Deposit,
                    Status = TransactionStatus.Completed,
                    Description = $"Refund for cancelled flight {booking.FlightNumber}"
                });
            }
            else
            {
                var installment = await _db.Installments
                    .FirstOrDefaultAsync(x => x.UserId == userId
                                              && x.AccountId == booking.AccountId
                                              && x.ProductName == $"Flight booking #{booking.Id}");

                refundAmount = installment?.PaidAmount ?? 0m;

                if (installment != null)
                    installment.Status = InstallmentStatus.Cancelled;

                if (refundAmount > 0)
                {
                    booking.Account.Balance += refundAmount;
                    _db.Transactions.Add(new Transaction
                    {
                        ToAccountId = booking.AccountId,
                        Amount = refundAmount,
                        Currency = booking.Currency,
                        Type = TransactionType.Deposit,
                        Status = TransactionStatus.Completed,
                        Description = $"Refund for cancelled installment flight {booking.FlightNumber}"
                    });
                }
            }

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await _notificationService.SendAsync(userId, "Flight booking cancelled", $"Flight booking {booking.FlightNumber} has been cancelled.", "FlightBooking");

            return new Response<string>(HttpStatusCode.OK, "Flight booking cancelled successfully");
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Cancel flight booking failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private static FlightBookingGetDto MapToDto(FlightBooking booking) => new()
    {
        Id = booking.Id,
        FlightNumber = booking.FlightNumber,
        FromCity = booking.FromCity,
        ToCity = booking.ToCity,
        DepartureDate = booking.DepartureDate,
        ReturnDate = booking.ReturnDate,
        PassengerCount = booking.PassengerCount,
        TotalPrice = booking.TotalPrice,
        IsInstallment = booking.IsInstallment,
        MonthlyPayment = booking.MonthlyPayment,
        Currency = booking.Currency.ToString(),
        Status = booking.Status,
        CreatedAt = booking.CreatedAt
    };
}
