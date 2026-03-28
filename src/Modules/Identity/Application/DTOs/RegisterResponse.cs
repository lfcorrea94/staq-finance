namespace StaqFinance.Modules.Identity.Application.DTOs;

public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    WorkspaceDto Workspace);
