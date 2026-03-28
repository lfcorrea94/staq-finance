using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaqFinance.Modules.Identity.Application.DTOs;
using StaqFinance.Modules.Identity.Application.Queries;
using System.Security.Claims;

namespace StaqFinance.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public sealed class MeController : ControllerBase
{
    private readonly IGetMeQueryHandler _getMeHandler;

    public MeController(IGetMeQueryHandler getMeHandler)
    {
        _getMeHandler = getMeHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdStr is null || !Guid.TryParse(userIdStr, out var userId))
            return Unauthorized();

        var result = await _getMeHandler.HandleAsync(new GetMeQuery(userId), cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code.StartsWith("User.") || result.Error.Code.StartsWith("Workspace.")
                ? NotFound(new { title = result.Error.Description })
                : StatusCode(StatusCodes.Status500InternalServerError);
        }

        return Ok(result.Value);
    }
}
