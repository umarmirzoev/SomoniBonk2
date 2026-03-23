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

public class CardService : ICardService
{
    private readonly AppDbContext _db;
    private readonly ILogger<CardService> _logger;

    public CardService(AppDbContext db, ILogger<CardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Response<CardGetDto>> GetByIdAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false)
    {
        try
        {
            IQueryable<Card> query = _db.Cards.AsNoTracking()
                .Include(x => x.Account);
            if (!isAdmin && requesterUserId.HasValue)
                query = query.Where(x => x.Account.UserId == requesterUserId.Value);

            var card = await query
                .FirstOrDefaultAsync(x => x.Id == id);
            if (card == null)
                return new Response<CardGetDto>(HttpStatusCode.NotFound, "Карта не найдена");

            return new Response<CardGetDto>(HttpStatusCode.OK, "Успешно", MapToDto(card));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCardById failed");
            return new Response<CardGetDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<PagedResult<CardGetDto>> GetAllAsync(CardFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;

        IQueryable<Card> query = _db.Cards.AsNoTracking()
            .Include(x => x.Account);

        if (filter?.UserId != null)
            query = query.Where(x => x.Account.UserId == filter.UserId.Value);
        if (filter?.AccountId != null)
            query = query.Where(x => x.AccountId == filter.AccountId);
        if (!string.IsNullOrEmpty(filter?.Status))
            query = query.Where(x => x.Status == Enum.Parse<CardStatus>(filter.Status, true));

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<CardGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<CardGetDto>> CreateAsync(Guid userId, CardInsertDto dto)
    {
        try
        {
            var account = await _db.Accounts
                .FirstOrDefaultAsync(x => x.Id == dto.AccountId && x.UserId == userId);
            if (account == null)
                return new Response<CardGetDto>(HttpStatusCode.NotFound, "Счёт не найден");

            var card = new Card
            {
                AccountId = dto.AccountId,
                CardNumber = GenerateCardNumber(),
                CardHolderName = dto.CardHolderName.ToUpper(),
                ExpiryDate = $"{DateTime.UtcNow.AddYears(3):MM/yy}",
                Cvv = GenerateCvv(),
                Status = CardStatus.Active
            };

            _db.Cards.Add(card);
            await _db.SaveChangesAsync();

            return new Response<CardGetDto>(HttpStatusCode.OK, "Карта успешно создана", MapToDto(card));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateCard failed");
            return new Response<CardGetDto>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> BlockAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false)
    {
        try
        {
            IQueryable<Card> query = _db.Cards.Include(x => x.Account);
            if (!isAdmin && requesterUserId.HasValue)
                query = query.Where(x => x.Account.UserId == requesterUserId.Value);

            var card = await query.FirstOrDefaultAsync(x => x.Id == id);
            if (card == null)
                return new Response<string>(HttpStatusCode.NotFound, "Карта не найдена");

            card.Status = CardStatus.Blocked;
            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Карта заблокирована");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BlockCard failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> UnblockAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false)
    {
        try
        {
            IQueryable<Card> query = _db.Cards.Include(x => x.Account);
            if (!isAdmin && requesterUserId.HasValue)
                query = query.Where(x => x.Account.UserId == requesterUserId.Value);

            var card = await query.FirstOrDefaultAsync(x => x.Id == id);
            if (card == null)
                return new Response<string>(HttpStatusCode.NotFound, "Карта не найдена");

            card.Status = CardStatus.Active;
            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Карта разблокирована");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UnblockCard failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    public async Task<Response<string>> DeleteAsync(Guid id, Guid? requesterUserId = null, bool isAdmin = false)
    {
        try
        {
            IQueryable<Card> query = _db.Cards.Include(x => x.Account);
            if (!isAdmin && requesterUserId.HasValue)
                query = query.Where(x => x.Account.UserId == requesterUserId.Value);

            var card = await query.FirstOrDefaultAsync(x => x.Id == id);
            if (card == null)
                return new Response<string>(HttpStatusCode.NotFound, "Карта не найдена");

            _db.Cards.Remove(card);
            await _db.SaveChangesAsync();
            return new Response<string>(HttpStatusCode.OK, "Карта удалена");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteCard failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Что-то пошло не так");
        }
    }

    private static string GenerateCardNumber()
    {
        var random = new Random();
        return $"4{random.Next(100, 999)}{random.Next(1000, 9999)}{random.Next(1000, 9999)}{random.Next(1000, 9999)}";
    }

    private static string GenerateCvv()
    {
        var random = new Random();
        return random.Next(100, 999).ToString();
    }

    private static CardGetDto MapToDto(Card c) => new()
    {
        Id = c.Id,
        AccountId = c.AccountId,
        CardNumber = $"**** **** **** {c.CardNumber[^4..]}",
        CardHolderName = c.CardHolderName,
        ExpiryDate = c.ExpiryDate,
        Status = c.Status.ToString(),
        CreatedAt = c.CreatedAt
    };
}
