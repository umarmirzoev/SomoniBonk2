using System.Net;
using System.Text.RegularExpressions;
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

public class BeneficiaryService : IBeneficiaryService
{
    private static readonly Regex AccountRegex = new("^[A-Za-z0-9]{8,34}$", RegexOptions.Compiled);
    private static readonly Regex CardRegex = new("^\\d{12,19}$", RegexOptions.Compiled);
    private static readonly Regex PhoneRegex = new("^\\+?\\d{7,15}$", RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<BeneficiaryService> _logger;

    public BeneficiaryService(AppDbContext db, IAuditLogService auditLogService, ILogger<BeneficiaryService> logger)
    {
        _db = db;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Response<BeneficiaryGetDto>> CreateAsync(Guid userId, BeneficiaryInsertDto dto)
    {
        try
        {
            var parseResult = ValidateBeneficiary(dto.TransferType, dto.AccountNumber, dto.CardNumber, dto.PhoneNumber);
            if (!parseResult.IsValid)
                return new Response<BeneficiaryGetDto>(HttpStatusCode.BadRequest, parseResult.Error!);

            var transferType = parseResult.TransferType!.Value;
            if (await ExistsDuplicateAsync(userId, transferType, dto.AccountNumber, dto.CardNumber, dto.PhoneNumber))
                return new Response<BeneficiaryGetDto>(HttpStatusCode.BadRequest, "Beneficiary already exists");

            var beneficiary = new Beneficiary
            {
                UserId = userId,
                FullName = dto.FullName.Trim(),
                BankName = dto.BankName.Trim(),
                AccountNumber = Normalize(dto.AccountNumber),
                CardNumber = Normalize(dto.CardNumber),
                PhoneNumber = Normalize(dto.PhoneNumber),
                TransferType = transferType,
                Nickname = Normalize(dto.Nickname),
                IsFavorite = false
            };

            _db.Beneficiaries.Add(beneficiary);
            await _db.SaveChangesAsync();
            await _auditLogService.LogAsync(userId, "BeneficiaryCreated", "", "", true);

            return new Response<BeneficiaryGetDto>(HttpStatusCode.OK, "Beneficiary created successfully", MapToDto(beneficiary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Create beneficiary failed");
            return new Response<BeneficiaryGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<BeneficiaryGetDto>> GetAllAsync(Guid userId, BeneficiaryFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;
        filter ??= new BeneficiaryFilter();

        IQueryable<Beneficiary> query = _db.Beneficiaries.AsNoTracking()
            .Where(x => x.UserId == userId);

        if (filter.IsFavorite.HasValue)
            query = query.Where(x => x.IsFavorite == filter.IsFavorite.Value);
        if (!string.IsNullOrWhiteSpace(filter.TransferType) && Enum.TryParse<BeneficiaryTransferType>(filter.TransferType, true, out var transferType))
            query = query.Where(x => x.TransferType == transferType);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(x => x.FullName.ToLower().Contains(search) ||
                                     x.BankName.ToLower().Contains(search) ||
                                     (x.Nickname != null && x.Nickname.ToLower().Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.IsFavorite)
            .ThenBy(x => x.FullName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<BeneficiaryGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<BeneficiaryGetDto>> GetByIdAsync(Guid userId, Guid id)
    {
        try
        {
            var beneficiary = await _db.Beneficiaries.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (beneficiary == null)
                return new Response<BeneficiaryGetDto>(HttpStatusCode.NotFound, "Beneficiary not found");

            return new Response<BeneficiaryGetDto>(HttpStatusCode.OK, "Success", MapToDto(beneficiary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get beneficiary by id failed");
            return new Response<BeneficiaryGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<BeneficiaryGetDto>> UpdateAsync(Guid userId, Guid id, BeneficiaryUpdateDto dto)
    {
        try
        {
            var beneficiary = await _db.Beneficiaries
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (beneficiary == null)
                return new Response<BeneficiaryGetDto>(HttpStatusCode.NotFound, "Beneficiary not found");

            var parseResult = ValidateBeneficiary(dto.TransferType, dto.AccountNumber, dto.CardNumber, dto.PhoneNumber);
            if (!parseResult.IsValid)
                return new Response<BeneficiaryGetDto>(HttpStatusCode.BadRequest, parseResult.Error!);

            var transferType = parseResult.TransferType!.Value;
            if (await ExistsDuplicateAsync(userId, transferType, dto.AccountNumber, dto.CardNumber, dto.PhoneNumber, id))
                return new Response<BeneficiaryGetDto>(HttpStatusCode.BadRequest, "Beneficiary already exists");

            beneficiary.FullName = dto.FullName.Trim();
            beneficiary.BankName = dto.BankName.Trim();
            beneficiary.AccountNumber = Normalize(dto.AccountNumber);
            beneficiary.CardNumber = Normalize(dto.CardNumber);
            beneficiary.PhoneNumber = Normalize(dto.PhoneNumber);
            beneficiary.TransferType = transferType;
            beneficiary.Nickname = Normalize(dto.Nickname);

            await _db.SaveChangesAsync();
            await _auditLogService.LogAsync(userId, "BeneficiaryUpdated", "", "", true);

            return new Response<BeneficiaryGetDto>(HttpStatusCode.OK, "Beneficiary updated successfully", MapToDto(beneficiary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update beneficiary failed");
            return new Response<BeneficiaryGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> DeleteAsync(Guid userId, Guid id)
    {
        try
        {
            var beneficiary = await _db.Beneficiaries
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (beneficiary == null)
                return new Response<string>(HttpStatusCode.NotFound, "Beneficiary not found");

            _db.Beneficiaries.Remove(beneficiary);
            await _db.SaveChangesAsync();
            await _auditLogService.LogAsync(userId, "BeneficiaryDeleted", "", "", true);

            return new Response<string>(HttpStatusCode.OK, "Beneficiary deleted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete beneficiary failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> SetFavoriteAsync(Guid userId, Guid id, bool isFavorite)
    {
        try
        {
            var beneficiary = await _db.Beneficiaries
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
            if (beneficiary == null)
                return new Response<string>(HttpStatusCode.NotFound, "Beneficiary not found");

            beneficiary.IsFavorite = isFavorite;
            await _db.SaveChangesAsync();
            await _auditLogService.LogAsync(userId, isFavorite ? "BeneficiaryFavorited" : "BeneficiaryUnfavorited", "", "", true);

            return new Response<string>(HttpStatusCode.OK, isFavorite ? "Beneficiary marked as favorite" : "Beneficiary removed from favorites");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set beneficiary favorite failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    private async Task<bool> ExistsDuplicateAsync(Guid userId, BeneficiaryTransferType transferType, string? accountNumber, string? cardNumber, string? phoneNumber, Guid? excludeId = null)
    {
        var query = _db.Beneficiaries.AsNoTracking()
            .Where(x => x.UserId == userId && x.TransferType == transferType);

        if (excludeId.HasValue)
            query = query.Where(x => x.Id != excludeId.Value);

        return transferType switch
        {
            BeneficiaryTransferType.Account => await query.AnyAsync(x => x.AccountNumber == Normalize(accountNumber)),
            BeneficiaryTransferType.Card => await query.AnyAsync(x => x.CardNumber == Normalize(cardNumber)),
            _ => await query.AnyAsync(x => x.PhoneNumber == Normalize(phoneNumber))
        };
    }

    private static (bool IsValid, BeneficiaryTransferType? TransferType, string? Error) ValidateBeneficiary(string transferTypeRaw, string? accountNumber, string? cardNumber, string? phoneNumber)
    {
        if (!Enum.TryParse<BeneficiaryTransferType>(transferTypeRaw, true, out var transferType))
            return (false, null, "Invalid transfer type");

        return transferType switch
        {
            BeneficiaryTransferType.Account when string.IsNullOrWhiteSpace(accountNumber) || !AccountRegex.IsMatch(accountNumber.Trim())
                => (false, null, "Invalid account number format"),
            BeneficiaryTransferType.Card when string.IsNullOrWhiteSpace(cardNumber) || !CardRegex.IsMatch(cardNumber.Trim())
                => (false, null, "Invalid card number format"),
            BeneficiaryTransferType.Phone when string.IsNullOrWhiteSpace(phoneNumber) || !PhoneRegex.IsMatch(phoneNumber.Trim())
                => (false, null, "Invalid phone number format"),
            _ => (true, transferType, null)
        };
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BeneficiaryGetDto MapToDto(Beneficiary beneficiary) => new()
    {
        Id = beneficiary.Id,
        FullName = beneficiary.FullName,
        BankName = beneficiary.BankName,
        AccountNumber = beneficiary.AccountNumber,
        CardNumber = beneficiary.CardNumber,
        PhoneNumber = beneficiary.PhoneNumber,
        TransferType = beneficiary.TransferType.ToString(),
        Nickname = beneficiary.Nickname,
        IsFavorite = beneficiary.IsFavorite,
        CreatedAt = beneficiary.CreatedAt
    };
}
