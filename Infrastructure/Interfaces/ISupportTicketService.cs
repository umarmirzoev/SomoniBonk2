using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface ISupportTicketService
{
    Task<Response<SupportTicketGetDto>> CreateTicketAsync(Guid userId, SupportTicketInsertDto dto);
    Task<PagedResult<SupportTicketGetDto>> GetMyTicketsAsync(Guid userId, SupportTicketFilter filter, PagedQuery pagedQuery);
    Task<PagedResult<SupportTicketGetDto>> GetAllTicketsAsync(SupportTicketFilter filter, PagedQuery pagedQuery);
    Task<Response<SupportTicketGetDto>> GetMyTicketByIdAsync(Guid userId, Guid id);
    Task<Response<SupportTicketGetDto>> GetTicketByIdAsync(Guid id);
    Task<Response<SupportMessageGetDto>> AddUserMessageAsync(Guid userId, Guid ticketId, SupportMessageInsertDto dto);
    Task<PagedResult<SupportMessageGetDto>> GetMyMessagesAsync(Guid userId, Guid ticketId, PagedQuery pagedQuery);
    Task<PagedResult<SupportMessageGetDto>> GetAdminMessagesAsync(Guid ticketId, PagedQuery pagedQuery);
    Task<Response<string>> AssignTicketAsync(Guid adminId, Guid ticketId);
    Task<Response<string>> UpdateStatusAsync(Guid adminId, Guid ticketId, SupportTicketStatusUpdateDto dto);
    Task<Response<SupportMessageGetDto>> AddAdminMessageAsync(Guid adminId, Guid ticketId, SupportMessageInsertDto dto);
}
