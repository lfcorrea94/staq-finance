using Microsoft.AspNetCore.Mvc;
using StaqFinance.Modules.Identity.Application.Commands;
using StaqFinance.Modules.Identity.Application.DTOs;

namespace StaqFinance.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IRegisterUserCommandHandler _registerHandler;
    private readonly ILoginCommandHandler _loginHandler;
    private readonly IRefreshTokenCommandHandler _refreshHandler;

    public AuthController(
        IRegisterUserCommandHandler registerHandler,
        ILoginCommandHandler loginHandler,
        IRefreshTokenCommandHandler refreshHandler)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _refreshHandler = refreshHandler;
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.Password,
            request.DisplayName,
            request.WorkspaceName);

        var result = await _registerHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Identity.DuplicateEmail" => Conflict(new { title = result.Error.Description }),
                _ => BadRequest(new { title = result.Error.Description })
            };
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _loginHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
            return Unauthorized(new { title = result.Error.Description });

        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var command = new RefreshTokenCommand(request.Token);
        var result = await _refreshHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
            return Unauthorized(new { title = result.Error.Description });

        return Ok(result.Value);
    }
}
