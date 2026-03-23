using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FlightBookingController(IFlightBookingService flightBookingService) : ControllerBase
{
    [HttpPost]
    public async Task<Response<FlightBookingGetDto>> Book(FlightBookingInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await flightBookingService.BookAsync(userId, dto);
    }

    [HttpGet("my")]
    public async Task<PagedResult<FlightBookingGetDto>> GetMy([FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await flightBookingService.GetMyBookingsAsync(userId, pagedQuery);
    }

    [HttpGet("{id:guid}")]
    public async Task<Response<FlightBookingGetDto>> GetById(Guid id)
        => await flightBookingService.GetByIdAsync(id);

    [HttpPatch("{id:guid}/cancel")]
    public async Task<Response<string>> Cancel(Guid id)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await flightBookingService.CancelAsync(userId, id);
    }
}
