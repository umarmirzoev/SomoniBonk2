using Microsoft.AspNetCore.Mvc;
using SomoniBank.Domain.DTOs;
using SomoniBank.Infrastructure.Interfaces;

namespace SomoniBank.API.Controllers;

[ApiController]
[Route("api/exchange-rates")]
public class ExchangeRatesController(IExchangeRateService exchangeRateService) : ControllerBase
{
    [HttpGet("latest")]
    public async Task<ActionResult<LatestExchangeRatesDto>> GetLatest(CancellationToken cancellationToken)
    {
        try
        {
            var result = await exchangeRateService.GetLatestRatesAsync(cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException)
        {
            return Ok(new LatestExchangeRatesDto
            {
                RateDate = DateTime.UtcNow.AddHours(5).Date,
                Source = "Unavailable",
                Rates = []
            });
        }
    }

    [HttpGet("convert")]
    public async Task<ActionResult<ExchangeConversionResultDto>> Convert(
        [FromQuery(Name = "from")] string fromCurrency,
        [FromQuery(Name = "to")] string toCurrency,
        [FromQuery] decimal amount,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await exchangeRateService.ConvertAsync(fromCurrency, toCurrency, amount, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                ["exchangeRates"] = [ex.Message]
            }));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: "Exchange rates are unavailable.", detail: ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }
}
