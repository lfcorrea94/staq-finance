using System.ComponentModel.DataAnnotations;

namespace StaqFinance.Modules.Identity.Application.DTOs;

public sealed record RefreshTokenRequest([Required] string Token);
