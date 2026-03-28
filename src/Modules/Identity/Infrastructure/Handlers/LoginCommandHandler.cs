using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Identity.Application.Commands;
using StaqFinance.Modules.Identity.Application.DTOs;
using StaqFinance.Modules.Identity.Application.Services;
using StaqFinance.Modules.Identity.Domain.Entities;

namespace StaqFinance.Modules.Identity.Infrastructure.Handlers;

internal sealed class LoginCommandHandler : ILoginCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public LoginCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<Result<AuthResponse>> HandleAsync(
        LoginCommand command,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.FindByEmailAsync(command.Email);

        if (user is null || !await _userManager.CheckPasswordAsync(user, command.Password))
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidCredentials", "Invalid credentials."));

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _tokenService.CreateRefreshTokenAsync(user.Id, cancellationToken);
        var expiresIn = (int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var min) ? min : 60) * 60;

        return Result.Success(new AuthResponse(accessToken, expiresIn, refreshToken));
    }
}
