namespace StaqFinance.Modules.Identity.Application.Commands;

public sealed record RegisterUserCommand(
    string Email,
    string Password,
    string DisplayName,
    string WorkspaceName);
