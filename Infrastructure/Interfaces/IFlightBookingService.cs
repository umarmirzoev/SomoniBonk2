using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IFlightBookingService
{
    Task<Response<FlightBookingGetDto>> BookAsync(Guid userId, FlightBookingInsertDto dto);
    Task<PagedResult<FlightBookingGetDto>> GetMyBookingsAsync(Guid userId, PagedQuery pagedQuery);
    Task<Response<FlightBookingGetDto>> GetByIdAsync(Guid id);
    Task<Response<string>> CancelAsync(Guid userId, Guid id);
}
