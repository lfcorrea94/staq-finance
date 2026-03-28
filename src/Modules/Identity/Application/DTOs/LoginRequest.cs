using System.ComponentModel.DataAnnotations;

namespace StaqFinance.Modules.Identity.Application.DTOs;

public sealed record LoginRequest(
    [Required][EmailAddress] string Email,
    [Required] string Password);
