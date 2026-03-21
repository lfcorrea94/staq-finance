using Microsoft.AspNetCore.Identity;

namespace StaqFinance.Modules.Identity.Domain.Entities;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
