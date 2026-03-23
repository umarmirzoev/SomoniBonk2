using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SomoniBank.Domain.DTOs;
using SomoniBank.Domain.Filtres;
using SomoniBank.Infrastructure.Interfaces;
using SomoniBank.Infrastructure.Responses;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TransactionController(ITransactionService transactionService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
    private bool IsAdmin => User.IsInRole("Admin");

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<PagedResult<TransactionGetDto>> GetAll([FromQuery] TransactionFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await transactionService.GetAllAsync(filter, pagedQuery, CurrentUserId, IsAdmin);

    [HttpGet("{id}")]
    public async Task<Response<TransactionGetDto>> GetById(Guid id)
        => await transactionService.GetByIdAsync(id, CurrentUserId, IsAdmin);

    [HttpGet("my")]
    public async Task<PagedResult<TransactionGetDto>> GetMyTransactions([FromQuery] TransactionFilter filter, [FromQuery] PagedQuery pagedQuery)
        => await transactionService.GetAllAsync(filter, pagedQuery, CurrentUserId);

    [HttpGet("recent")]
    public async Task<PagedResult<TransactionGetDto>> GetRecentTransactions(CancellationToken cancellationToken = default)
        => await transactionService.GetAllAsync(new TransactionFilter(), new PagedQuery { Page = 1, PageSize = 10 }, CurrentUserId);

    [HttpPost("transfer")]
    public async Task<Response<string>> Transfer(TransferDto dto)
        => await transactionService.TransferAsync(CurrentUserId, dto);

    [HttpPost("deposit")]
    public async Task<Response<string>> DepositMoney(DepositMoneyDto dto)
        => await transactionService.DepositMoneyAsync(CurrentUserId, dto);

    [HttpPost("withdraw")]
    public async Task<Response<string>> WithdrawMoney(WithdrawMoneyDto dto)
        => await transactionService.WithdrawMoneyAsync(CurrentUserId, dto);

    [HttpPost("exchange")]
    public async Task<Response<string>> ExchangeCurrency(CurrencyExchangeDto dto)
        => await transactionService.ExchangeCurrencyAsync(CurrentUserId, dto);
}
