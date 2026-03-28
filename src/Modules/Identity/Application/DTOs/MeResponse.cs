namespace StaqFinance.Modules.Identity.Application.DTOs;

public sealed record MeResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    DateTime CreatedAt,
    WorkspaceDto Workspace);
