using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class BillPaymentController(IBillPaymentService billPaymentService) : ControllerBase
{
    [HttpGet("categories")]
    public async Task<Response<List<BillCategoryGetDto>>> GetCategories()
        => await billPaymentService.GetCategoriesAsync();

    [HttpGet("categories/{categoryId:guid}/providers")]
    public async Task<Response<List<BillProviderGetDto>>> GetProvidersByCategory(Guid categoryId)
        => await billPaymentService.GetProvidersByCategoryAsync(categoryId);

    [HttpPost("pay")]
    public async Task<Response<BillPaymentGetDto>> PayBill(BillPaymentInsertDto dto)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await billPaymentService.PayBillAsync(userId, dto);
    }

    [HttpGet("my")]
    public async Task<PagedResult<BillPaymentGetDto>> GetMyPayments([FromQuery] BillPaymentFilter filter, [FromQuery] PagedQuery pagedQuery)
    {
        var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
        return await billPaymentService.GetMyPaymentsAsync(userId, filter, pagedQuery);
    }
}
