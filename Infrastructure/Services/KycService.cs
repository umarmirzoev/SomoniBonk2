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

public class KycService : IKycService
{
    private readonly AppDbContext _db;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<KycService> _logger;

    public KycService(
        AppDbContext db,
        INotificationService notificationService,
        IAuditLogService auditLogService,
        ILogger<KycService> logger)
    {
        _db = db;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _logger = logger;
    }

    public async Task<Response<KycProfileGetDto>> SubmitAsync(Guid userId, KycSubmitDto dto)
    {
        try
        {
            var validationError = ValidateKyc(dto);
            if (validationError != null)
                return new Response<KycProfileGetDto>(HttpStatusCode.BadRequest, validationError);

            var existing = await _db.KycProfiles
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (existing != null && (existing.Status == KycStatus.Pending || existing.Status == KycStatus.UnderReview))
                return new Response<KycProfileGetDto>(HttpStatusCode.BadRequest, "You already have an active KYC submission");

            if (existing != null && existing.Status == KycStatus.Approved)
                return new Response<KycProfileGetDto>(HttpStatusCode.BadRequest, "Your KYC profile is already approved");

            if (await _db.KycProfiles.AsNoTracking().AnyAsync(x => x.UserId != userId && x.NationalIdNumber == dto.NationalIdNumber))
                return new Response<KycProfileGetDto>(HttpStatusCode.BadRequest, "National ID number is already used");

            KycProfile profile;
            if (existing == null)
            {
                profile = new KycProfile
                {
                    UserId = userId
                };
                _db.KycProfiles.Add(profile);
            }
            else
            {
                profile = existing;
                _db.KycDocuments.RemoveRange(profile.Documents);
            }

            ApplyKyc(profile, dto);
            profile.Status = KycStatus.Pending;
            profile.SubmittedAt = DateTime.UtcNow;
            profile.ReviewedAt = null;
            profile.ReviewedByAdminId = null;
            profile.RejectionReason = null;

            profile.Documents =
            [
                new KycDocument { KycProfileId = profile.Id, Type = KycDocumentType.Selfie, FileUrl = dto.SelfieImageUrl },
                new KycDocument { KycProfileId = profile.Id, Type = KycDocumentType.DocumentFront, FileUrl = dto.DocumentFrontUrl },
                new KycDocument { KycProfileId = profile.Id, Type = KycDocumentType.DocumentBack, FileUrl = dto.DocumentBackUrl }
            ];

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "KYC submitted", "Your identity verification request has been submitted for review.", "KYC");
            await _auditLogService.LogAsync(userId, "KycSubmitted", "", "", true);

            return new Response<KycProfileGetDto>(HttpStatusCode.OK, "KYC submitted successfully", MapToDto(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Submit KYC failed");
            return new Response<KycProfileGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<KycStatusGetDto>> GetMyStatusAsync(Guid userId)
    {
        try
        {
            var profile = await _db.KycProfiles.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null)
                return new Response<KycStatusGetDto>(HttpStatusCode.NotFound, "KYC profile not found");

            return new Response<KycStatusGetDto>(HttpStatusCode.OK, "Success", new KycStatusGetDto
            {
                Status = profile.Status.ToString(),
                SubmittedAt = profile.SubmittedAt,
                ReviewedAt = profile.ReviewedAt,
                RejectionReason = profile.RejectionReason
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get KYC status failed");
            return new Response<KycStatusGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<KycProfileGetDto>> GetMyProfileAsync(Guid userId)
    {
        try
        {
            var profile = await _db.KycProfiles.AsNoTracking()
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null)
                return new Response<KycProfileGetDto>(HttpStatusCode.NotFound, "KYC profile not found");

            return new Response<KycProfileGetDto>(HttpStatusCode.OK, "Success", MapToDto(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get my KYC profile failed");
            return new Response<KycProfileGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<KycProfileGetDto>> UpdateAsync(Guid userId, KycUpdateDto dto)
    {
        try
        {
            var validationError = ValidateKyc(dto);
            if (validationError != null)
                return new Response<KycProfileGetDto>(HttpStatusCode.BadRequest, validationError);

            var profile = await _db.KycProfiles
                .Include(x => x.Documents)
                .FirstOrDefaultAsync(x => x.UserId == userId);
            if (profile == null)
                return new Response<KycProfileGetDto>(HttpStatusCode.NotFound, "KYC profile not found");

            if (profile.Status == KycStatus.Approved)
                return new Response<KycProfileGetDto>(HttpStatusCode.BadRequest, "Approved KYC profile cannot be updated");

            if (await _db.KycProfiles.AsNoTracking().AnyAsync(x => x.UserId != userId && x.NationalIdNumber == dto.NationalIdNumber))
                return new Response<KycProfileGetDto>(HttpStatusCode.BadRequest, "National ID number is already used");

            ApplyKyc(profile, dto);
            profile.Status = KycStatus.Pending;
            profile.SubmittedAt = DateTime.UtcNow;
            profile.ReviewedAt = null;
            profile.ReviewedByAdminId = null;
            profile.RejectionReason = null;

            _db.KycDocuments.RemoveRange(profile.Documents);
            profile.Documents =
            [
                new KycDocument { KycProfileId = profile.Id, Type = KycDocumentType.Selfie, FileUrl = dto.SelfieImageUrl },
                new KycDocument { KycProfileId = profile.Id, Type = KycDocumentType.DocumentFront, FileUrl = dto.DocumentFrontUrl },
                new KycDocument { KycProfileId = profile.Id, Type = KycDocumentType.DocumentBack, FileUrl = dto.DocumentBackUrl }
            ];

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(userId, "KYC updated", "Your KYC profile has been updated and sent for review again.", "KYC");
            await _auditLogService.LogAsync(userId, "KycUpdated", "", "", true);

            return new Response<KycProfileGetDto>(HttpStatusCode.OK, "KYC updated successfully", MapToDto(profile));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update KYC failed");
            return new Response<KycProfileGetDto>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<PagedResult<KycProfileGetDto>> GetPendingAsync(PagedQuery pagedQuery)
        => await GetAllAsync(new KycFilter { Status = KycStatus.Pending.ToString() }, pagedQuery);

    public async Task<PagedResult<KycProfileGetDto>> GetAllAsync(KycFilter filter, PagedQuery pagedQuery)
    {
        var page = pagedQuery.Page <= 0 ? 1 : pagedQuery.Page;
        var pageSize = pagedQuery.PageSize <= 0 ? 10 : pagedQuery.PageSize;
        filter ??= new KycFilter();

        IQueryable<KycProfile> query = _db.KycProfiles.AsNoTracking();

        if (filter.UserId.HasValue)
            query = query.Where(x => x.UserId == filter.UserId.Value);
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<KycStatus>(filter.Status, true, out var status))
            query = query.Where(x => x.Status == status);

        var totalCount = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<KycProfileGetDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<Response<string>> ApproveAsync(Guid adminId, Guid id)
    {
        try
        {
            var profile = await _db.KycProfiles.FirstOrDefaultAsync(x => x.Id == id);
            if (profile == null)
                return new Response<string>(HttpStatusCode.NotFound, "KYC profile not found");

            if (profile.Status == KycStatus.Approved)
                return new Response<string>(HttpStatusCode.BadRequest, "KYC profile is already approved");

            profile.Status = KycStatus.Approved;
            profile.ReviewedAt = DateTime.UtcNow;
            profile.ReviewedByAdminId = adminId;
            profile.RejectionReason = null;

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(profile.UserId, "KYC approved", "Your identity has been verified successfully.", "KYC");
            await _auditLogService.LogAsync(adminId, "KycApproved", "", "", true);

            return new Response<string>(HttpStatusCode.OK, "KYC approved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Approve KYC failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<Response<string>> RejectAsync(Guid adminId, Guid id, KycReviewDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.RejectionReason))
                return new Response<string>(HttpStatusCode.BadRequest, "Rejection reason is required");

            var profile = await _db.KycProfiles.FirstOrDefaultAsync(x => x.Id == id);
            if (profile == null)
                return new Response<string>(HttpStatusCode.NotFound, "KYC profile not found");

            profile.Status = KycStatus.Rejected;
            profile.ReviewedAt = DateTime.UtcNow;
            profile.ReviewedByAdminId = adminId;
            profile.RejectionReason = dto.RejectionReason.Trim();

            await _db.SaveChangesAsync();
            await _notificationService.SendAsync(profile.UserId, "KYC rejected", $"Your KYC request was rejected. Reason: {profile.RejectionReason}", "KYC");
            await _auditLogService.LogAsync(adminId, "KycRejected", "", "", true);

            return new Response<string>(HttpStatusCode.OK, "KYC rejected successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reject KYC failed");
            return new Response<string>(HttpStatusCode.InternalServerError, "Something went wrong");
        }
    }

    public async Task<bool> HasApprovedKycAsync(Guid userId)
        => await _db.KycProfiles.AsNoTracking().AnyAsync(x => x.UserId == userId && x.Status == KycStatus.Approved);

    private static void ApplyKyc(KycProfile profile, KycSubmitDto dto)
    {
        profile.FullName = dto.FullName.Trim();
        profile.DateOfBirth = dto.DateOfBirth;
        profile.NationalIdNumber = dto.NationalIdNumber.Trim();
        profile.PassportNumber = dto.PassportNumber.Trim();
        profile.Address = dto.Address.Trim();
        profile.SelfieImageUrl = dto.SelfieImageUrl.Trim();
        profile.DocumentFrontUrl = dto.DocumentFrontUrl.Trim();
        profile.DocumentBackUrl = dto.DocumentBackUrl.Trim();
    }

    private static void ApplyKyc(KycProfile profile, KycUpdateDto dto)
        => ApplyKyc(profile, new KycSubmitDto
        {
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            NationalIdNumber = dto.NationalIdNumber,
            PassportNumber = dto.PassportNumber,
            Address = dto.Address,
            SelfieImageUrl = dto.SelfieImageUrl,
            DocumentFrontUrl = dto.DocumentFrontUrl,
            DocumentBackUrl = dto.DocumentBackUrl
        });

    private static string? ValidateKyc(KycSubmitDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.NationalIdNumber) ||
            string.IsNullOrWhiteSpace(dto.PassportNumber) || string.IsNullOrWhiteSpace(dto.Address) ||
            string.IsNullOrWhiteSpace(dto.SelfieImageUrl) || string.IsNullOrWhiteSpace(dto.DocumentFrontUrl) ||
            string.IsNullOrWhiteSpace(dto.DocumentBackUrl))
            return "All KYC fields are required";

        if (dto.DateOfBirth > DateTime.UtcNow.AddYears(-18))
            return "User must be at least 18 years old";

        return null;
    }

    private static string? ValidateKyc(KycUpdateDto dto)
        => ValidateKyc(new KycSubmitDto
        {
            FullName = dto.FullName,
            DateOfBirth = dto.DateOfBirth,
            NationalIdNumber = dto.NationalIdNumber,
            PassportNumber = dto.PassportNumber,
            Address = dto.Address,
            SelfieImageUrl = dto.SelfieImageUrl,
            DocumentFrontUrl = dto.DocumentFrontUrl,
            DocumentBackUrl = dto.DocumentBackUrl
        });

    private static KycProfileGetDto MapToDto(KycProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        FullName = profile.FullName,
        DateOfBirth = profile.DateOfBirth,
        NationalIdNumber = profile.NationalIdNumber,
        PassportNumber = profile.PassportNumber,
        Address = profile.Address,
        SelfieImageUrl = profile.SelfieImageUrl,
        DocumentFrontUrl = profile.DocumentFrontUrl,
        DocumentBackUrl = profile.DocumentBackUrl,
        Status = profile.Status.ToString(),
        SubmittedAt = profile.SubmittedAt,
        ReviewedAt = profile.ReviewedAt,
        ReviewedByAdminId = profile.ReviewedByAdminId,
        RejectionReason = profile.RejectionReason
    };
}
