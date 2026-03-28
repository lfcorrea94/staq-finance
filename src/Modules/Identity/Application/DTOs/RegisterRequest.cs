using System.ComponentModel.DataAnnotations;

namespace StaqFinance.Modules.Identity.Application.DTOs;

public sealed record RegisterRequest(
    [Required][EmailAddress][MaxLength(254)] string Email,
    [Required][MinLength(8)][MaxLength(100)] string Password,
    [Required][MinLength(1)][MaxLength(100)] string DisplayName,
    [Required][MinLength(2)][MaxLength(80)] string WorkspaceName);
