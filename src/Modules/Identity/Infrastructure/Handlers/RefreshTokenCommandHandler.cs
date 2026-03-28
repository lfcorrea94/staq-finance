using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Identity.Application.Commands;
using StaqFinance.Modules.Identity.Application.DTOs;
using StaqFinance.Modules.Identity.Application.Services;
using StaqFinance.Modules.Identity.Domain.Entities;

namespace StaqFinance.Modules.Identity.Infrastructure.Handlers;

internal sealed class RefreshTokenCommandHandler : IRefreshTokenCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    public RefreshTokenCommandHandler(
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    public async Task<Result<AuthResponse>> HandleAsync(
        RefreshTokenCommand command,
        CancellationToken cancellationToken = default)
    {
        var (isValid, userId, newRefreshToken) =
            await _tokenService.ValidateAndRotateAsync(command.Token, cancellationToken);

        if (!isValid)
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidToken", "Refresh token is invalid or expired."));

        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
            return Result.Failure<AuthResponse>(
                Error.Unauthorized("Auth.InvalidToken", "Refresh token is invalid or expired."));

        var accessToken = _tokenService.GenerateAccessToken(user);
        var expiresIn = (int.TryParse(_configuration["Jwt:ExpiresInMinutes"], out var min) ? min : 60) * 60;

        return Result.Success(new AuthResponse(accessToken, expiresIn, newRefreshToken));
    }
}
