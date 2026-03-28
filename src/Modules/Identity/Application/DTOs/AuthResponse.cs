namespace StaqFinance.Modules.Identity.Application.DTOs;

public sealed record AuthResponse(
    string AccessToken,
    int ExpiresIn,
    string RefreshToken);
