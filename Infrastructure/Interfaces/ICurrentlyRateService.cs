using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.Infrastructure.Interfaces;

public interface ICurrencyRateService
{
    Task<Response<CurrencyRateGetDto>> GetByIdAsync(Guid id);
    Task<PagedResult<CurrencyRateGetDto>> GetAllAsync(CurrencyRateFilter filter, PagedQuery pagedQuery);
    Task<Response<string>> CreateOrUpdateAsync(CurrencyRateInsertDto dto);
    Task<Response<decimal>> ConvertAsync(CurrencyConvertDto dto);
}