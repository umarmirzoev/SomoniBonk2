using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
public class CurrencyRateController(ICurrencyRateService currencyRateService) : ControllerBase
{
    [HttpGet]
    public async Task<PagedResult<CurrencyRateGetDto>> GetAll([FromQuery] CurrencyRateFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await currencyRateService.GetAllAsync(filter, pagedQuery);

    [HttpGet("{id}")]
    public async Task<Response<CurrencyRateGetDto>> GetById(Guid id)
        => await currencyRateService.GetByIdAsync(id);

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<Response<string>> CreateOrUpdate(CurrencyRateInsertDto dto)
        => await currencyRateService.CreateOrUpdateAsync(dto);

    [HttpPost("convert")]
    public async Task<Response<decimal>> Convert(CurrencyConvertDto dto)
        => await currencyRateService.ConvertAsync(dto);
}