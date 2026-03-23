using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IBeneficiaryService
{
    Task<Response<BeneficiaryGetDto>> CreateAsync(Guid userId, BeneficiaryInsertDto dto);
    Task<PagedResult<BeneficiaryGetDto>> GetAllAsync(Guid userId, BeneficiaryFilter filter, PagedQuery pagedQuery);
    Task<Response<BeneficiaryGetDto>> GetByIdAsync(Guid userId, Guid id);
    Task<Response<BeneficiaryGetDto>> UpdateAsync(Guid userId, Guid id, BeneficiaryUpdateDto dto);
    Task<Response<string>> DeleteAsync(Guid userId, Guid id);
    Task<Response<string>> SetFavoriteAsync(Guid userId, Guid id, bool isFavorite);
}
