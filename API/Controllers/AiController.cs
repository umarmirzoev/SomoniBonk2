using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SomoniBank.Application.AI.DTOs;
using SomoniBank.Application.AI.Interfaces;

namespace SomoniBank.API.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class AiController(IAiService aiService, IAiContextService aiContextService) : ControllerBase
{
    [HttpPost("ask")]
    [ProducesResponseType(typeof(AiAskResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AiAskResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(AiAskResponseDto), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(AiAskResponseDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AiAskResponseDto>> Ask([FromBody] AiAskRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        if (request is null || string.IsNullOrWhiteSpace(request.UserQuestion))
        {
            return BadRequest(new AiAskResponseDto
            {
                Success = false,
                Answer = string.Empty,
                Error = "UserQuestion is required."
            });
        }

        try
        {
            var context = await aiContextService.BuildContextAsync(cancellationToken);
            var response = await aiService.AskAsync(request, context, cancellationToken);

            if (!response.Success)
                return StatusCode(StatusCodes.Status503ServiceUnavailable, response);

            return Ok(response);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized(new AiAskResponseDto
            {
                Success = false,
                Answer = string.Empty,
                Error = "Authentication is required."
            });
        }
    }
}
