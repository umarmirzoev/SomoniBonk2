using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IKycService
{
    Task<Response<KycProfileGetDto>> SubmitAsync(Guid userId, KycSubmitDto dto);
    Task<Response<KycStatusGetDto>> GetMyStatusAsync(Guid userId);
    Task<Response<KycProfileGetDto>> GetMyProfileAsync(Guid userId);
    Task<Response<KycProfileGetDto>> UpdateAsync(Guid userId, KycUpdateDto dto);
    Task<PagedResult<KycProfileGetDto>> GetPendingAsync(PagedQuery pagedQuery);
    Task<PagedResult<KycProfileGetDto>> GetAllAsync(KycFilter filter, PagedQuery pagedQuery);
    Task<Response<string>> ApproveAsync(Guid adminId, Guid id);
    Task<Response<string>> RejectAsync(Guid adminId, Guid id, KycReviewDto dto);
    Task<bool> HasApprovedKycAsync(Guid userId);
}
