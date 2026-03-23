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

public class SupportTicketService : ISupportTicketService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<SupportTicketService> _logger;

    public SupportTicketService(
        AppDbContext db,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        ILogger<SupportTicketService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Response<SupportTicketGetDto>> CreateTicketAsync(Guid userId, SupportTicketInsertDto dto)
    {
        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Subject) || string.IsNullOrWhiteSpace(dto.Category) || string.IsNullOrWhiteSpace(dto.Description))
                return new Response<SupportTicketGetDto>(HttpStatusCode.BadRequest, "Subject, category, and description are required");

            if (!Enum.TryParse<SupportPriority>(dto.Priority, true, out var priority))
                return new Response<SupportTicketGetDto>(HttpStatusCode.BadRequest, "Invalid priority");

            var ticket = new SupportTicket
            {
                UserId = userId,
                Subject = dto.Subject.Trim(),
                Category = dto.Category.Trim(),
                Description = dto.Description.Trim(),
                Priority = priority,
                Status = SupportTicketStatus.Open
            };

            _db.SupportTickets.Add(ticket);
            await _db.SaveChangesAsync();

            _db.SupportMessages.Add(new SupportMessage
            {
                TicketId = ticket.Id,
                SenderUserId = userId,
                MessageText = ticket.Description
            });

            await _db.SaveChangesAsync();
            await dbTransaction.CommitAsync();
            await _notificationService.SendAsync(userId, "Support ticket created", $"Support ticket '{ticket.Subject}' has been created.", "Support");
            await _auditLogService.LogAsync(userId, "SupportTicketCreated", "", "", true);

            return new Response<SupportTicketGetDto>(HttpStatusCode.OK, "Support ticket created successfully", MapTicket(ticket));
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync();
            _logger.LogError(ex, "Create support ticket failed");
            return new Response<SupportTicketGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<SupportTicketGetDto>> GetMyTicketsAsync(Guid userId, SupportTicketFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;
        filter ??= new SupportTicketFilter();

        IQueryable<SupportTicket> query = _db.SupportTickets.AsNoTracking()
            .Where(x => x.UserId == userId);

        query = ApplySupportFilter(query, filter);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SupportTicketGetDto>
        {
            Items = items.Select(MapTicket).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<PagedResult<SupportTicketGetDto>> GetAllTicketsAsync(SupportTicketFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;
        filter ??= new SupportTicketFilter();

        IQueryable<SupportTicket> query = _db.SupportTickets.AsNoTracking();
        query = ApplySupportFilter(query, filter);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SupportTicketGetDto>
        {
            Items = items.Select(MapTicket).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<SupportTicketGetDto>> GetMyTicketByIdAsync(Guid userId, Guid id)
    {
        try
        {
            var ticket = await _db.SupportTickets.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (ticket == null)
                return new Response<SupportTicketGetDto>(HttpStatusCode.NotFound, "Support ticket not found");

            return new Response<SupportTicketGetDto>(HttpStatusCode.OK, "Success", MapTicket(ticket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get support ticket by id failed");
            return new Response<SupportTicketGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<SupportTicketGetDto>> GetTicketByIdAsync(Guid id)
    {
        try
        {
            var ticket = await _db.SupportTickets.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            if (ticket == null)
                return new Response<SupportTicketGetDto>(HttpStatusCode.NotFound, "Support ticket not found");

            return new Response<SupportTicketGetDto>(HttpStatusCode.OK, "Success", MapTicket(ticket));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get admin support ticket by id failed");
            return new Response<SupportTicketGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<SupportMessageGetDto>> AddUserMessageAsync(Guid userId, Guid ticketId, SupportMessageInsertDto dto)
    {
        try
        {
            var ticket = await _db.SupportTickets
                .FirstOrDefaultAsync(x => x.Id == ticketId && x.UserId == userId);
            if (ticket == null)
                return new Response<SupportMessageGetDto>(HttpStatusCode.NotFound, "Support ticket not found");

            if (ticket.Status == SupportTicketStatus.Closed)
                return new Response<SupportMessageGetDto>(HttpStatusCode.BadRequest, "Closed ticket cannot receive new messages");

            if (string.IsNullOrWhiteSpace(dto.MessageText))
                return new Response<SupportMessageGetDto>(HttpStatusCode.BadRequest, "Message text is required");

            var message = new SupportMessage
            {
                TicketId = ticketId,
                SenderUserId = userId,
                MessageText = dto.MessageText.Trim()
            };

            ticket.UpdatedAt = DateTime.UtcNow;
            _db.SupportMessages.Add(message);
            await _db.SaveChangesAsync();

            await _notificationService.SendAsync(userId, "Support message sent", $"Your message for ticket '{ticket.Subject}' has been sent.", "Support");
            return new Response<SupportMessageGetDto>(HttpStatusCode.OK, "Message sent successfully", MapMessage(message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add user support message failed");
            return new Response<SupportMessageGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<SupportMessageGetDto>> GetMyMessagesAsync(Guid userId, Guid ticketId, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var ticketExists = await _db.SupportTickets.AsNoTracking()
            .AnyAsync(x => x.Id == ticketId && x.UserId == userId);
        if (!ticketExists)
            return new PagedResult<SupportMessageGetDto>();

        var unreadAdminMessages = await _db.SupportMessages
            .Where(x => x.TicketId == ticketId && x.SenderAdminId != null && !x.IsRead)
            .ToListAsync();

        foreach (var message in unreadAdminMessages)
            message.IsRead = true;

        if (unreadAdminMessages.Count > 0)
            await _db.SaveChangesAsync();

        var query = _db.SupportMessages.AsNoTracking()
            .Where(x => x.TicketId == ticketId);

        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(x => x.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SupportMessageGetDto>
        {
            Items = items.Select(MapMessage).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<PagedResult<SupportMessageGetDto>> GetAdminMessagesAsync(Guid ticketId, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        var ticketExists = await _db.SupportTickets.AsNoTracking().AnyAsync(x => x.Id == ticketId);
        if (!ticketExists)
            return new PagedResult<SupportMessageGetDto>();

        var query = _db.SupportMessages.AsNoTracking()
            .Where(x => x.TicketId == ticketId);

        var totalCount = await query.CountAsync();
        var items = await query.OrderBy(x => x.SentAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<SupportMessageGetDto>
        {
            Items = items.Select(MapMessage).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<string>> AssignTicketAsync(Guid adminId, Guid ticketId)
    {
        try
        {
            var ticket = await _db.SupportTickets.FirstOrDefaultAsync(x => x.Id == ticketId);
            if (ticket == null)
                return new Response<string>(HttpStatusCode.NotFound, "Support ticket not found");

            ticket.AssignedAdminId = adminId;
            ticket.Status = ticket.Status == SupportTicketStatus.Open ? SupportTicketStatus.InProgress : ticket.Status;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(ticket.UserId, "Support ticket assigned", $"Your support ticket '{ticket.Subject}' has been assigned to an admin.", "Support");
            await _auditLogService.LogAsync(adminId, "SupportTicketAssigned", "", "", true);

            return new Response<string>(HttpStatusCode.OK, "Support ticket assigned successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assign support ticket failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> UpdateStatusAsync(Guid adminId, Guid ticketId, SupportTicketStatusUpdateDto dto)
    {
        try
        {
            if (!Enum.TryParse<SupportTicketStatus>(dto.Status, true, out var status))
                return new Response<string>(HttpStatusCode.BadRequest, "Invalid support ticket status");

            var ticket = await _db.SupportTickets.FirstOrDefaultAsync(x => x.Id == ticketId);
            if (ticket == null)
                return new Response<string>(HttpStatusCode.NotFound, "Support ticket not found");

            ticket.Status = status;
            ticket.UpdatedAt = DateTime.UtcNow;
            ticket.ClosedAt = status == SupportTicketStatus.Closed ? DateTime.UtcNow : null;

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(ticket.UserId, "Support ticket updated", $"Your support ticket '{ticket.Subject}' status is now {status}.", "Support");
            await _auditLogService.LogAsync(adminId, "SupportTicketStatusUpdated", "", "", true);

            return new Response<string>(HttpStatusCode.OK, "Support ticket status updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update support ticket status failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<SupportMessageGetDto>> AddAdminMessageAsync(Guid adminId, Guid ticketId, SupportMessageInsertDto dto)
    {
        try
        {
            var ticket = await _db.SupportTickets.FirstOrDefaultAsync(x => x.Id == ticketId);
            if (ticket == null)
                return new Response<SupportMessageGetDto>(HttpStatusCode.NotFound, "Support ticket not found");

            if (ticket.Status == SupportTicketStatus.Closed)
                return new Response<SupportMessageGetDto>(HttpStatusCode.BadRequest, "Closed ticket cannot receive new messages");

            if (string.IsNullOrWhiteSpace(dto.MessageText))
                return new Response<SupportMessageGetDto>(HttpStatusCode.BadRequest, "Message text is required");

            var message = new SupportMessage
            {
                TicketId = ticketId,
                SenderAdminId = adminId,
                MessageText = dto.MessageText.Trim()
            };

            ticket.UpdatedAt = DateTime.UtcNow;
            ticket.Status = ticket.Status == SupportTicketStatus.Open ? SupportTicketStatus.InProgress : ticket.Status;
            _db.SupportMessages.Add(message);
            await _db.SaveChangesAsync();

            await _notificationService.SendAsync(ticket.UserId, "New support reply", $"Bank support replied to ticket '{ticket.Subject}'.", "Support");
            await _auditLogService.LogAsync(adminId, "SupportAdminReply", "", "", true);

            return new Response<SupportMessageGetDto>(HttpStatusCode.OK, "Message sent successfully", MapMessage(message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add admin support message failed");
            return new Response<SupportMessageGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private static IQueryable<SupportTicket> ApplySupportFilter(IQueryable<SupportTicket> query, SupportTicketFilter filter)
    {
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<SupportTicketStatus>(filter.Status, true, out var status))
            query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(filter.Priority) && Enum.TryParse<SupportPriority>(filter.Priority, true, out var priority))
            query = query.Where(x => x.Priority == priority);
        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(x => x.Category == filter.Category);

        return query;
    }

    private static SupportTicketGetDto MapTicket(SupportTicket ticket) => new()
    {
        Id = ticket.Id,
        UserId = ticket.UserId,
        Subject = ticket.Subject,
        Category = ticket.Category,
        Description = ticket.Description,
        Status = ticket.Status.ToString(),
        Priority = ticket.Priority.ToString(),
        CreatedAt = ticket.CreatedAt,
        UpdatedAt = ticket.UpdatedAt,
        ClosedAt = ticket.ClosedAt,
        AssignedAdminId = ticket.AssignedAdminId
    };

    private static SupportMessageGetDto MapMessage(SupportMessage message) => new()
    {
        Id = message.Id,
        TicketId = message.TicketId,
        SenderUserId = message.SenderUserId,
        SenderAdminId = message.SenderAdminId,
        MessageText = message.MessageText,
        SentAt = message.SentAt,
        IsRead = message.IsRead
    };
}
