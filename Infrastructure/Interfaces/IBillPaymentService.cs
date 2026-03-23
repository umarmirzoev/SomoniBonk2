using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface IBillPaymentService
{
    Task<Response<List<BillCategoryGetDto>>> GetCategoriesAsync();
    Task<Response<List<BillProviderGetDto>>> GetProvidersByCategoryAsync(Guid categoryId);
    Task<Response<BillPaymentGetDto>> PayBillAsync(Guid userId, BillPaymentInsertDto dto);
    Task<PagedResult<BillPaymentGetDto>> GetMyPaymentsAsync(Guid userId, BillPaymentFilter filter, PagedQuery pagedQuery);
}
