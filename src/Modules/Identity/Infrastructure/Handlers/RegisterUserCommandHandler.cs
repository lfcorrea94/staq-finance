using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using StaqFinance.BuildingBlocks;
using StaqFinance.Modules.Identity.Application.Commands;
using StaqFinance.Modules.Identity.Application.DTOs;
using StaqFinance.Modules.Identity.Application.Services;
using StaqFinance.Modules.Identity.Domain.Entities;
using StaqFinance.Modules.Tenancy.Domain.Entities;

namespace StaqFinance.Modules.Identity.Infrastructure.Handlers;

internal sealed class RegisterUserCommandHandler : IRegisterUserCommandHandler
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ISlugService _slugService;
    private readonly DbContext _context;

    public RegisterUserCommandHandler(
        UserManager<ApplicationUser> userManager,
        ISlugService slugService,
        DbContext context)
    {
        _userManager = userManager;
        _slugService = slugService;
        _context = context;
    }

    public async Task<Result<RegisterResponse>> HandleAsync(
        RegisterUserCommand command,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var slug = await _slugService.GenerateUniqueSlugAsync(command.WorkspaceName, cancellationToken);

            if (slug.Length < 3 || slug.Length > 40)
                return await RollbackAsync<RegisterResponse>(tx, cancellationToken,
                    Error.Validation("Slug.Invalid", "Workspace name generates an invalid slug (must produce 3–40 characters)."));

            var user = new ApplicationUser
            {
                UserName = command.Email,
                Email = command.Email,
                DisplayName = command.DisplayName,
                CreatedAt = DateTime.UtcNow
            };

            var identityResult = await _userManager.CreateAsync(user, command.Password);

            if (!identityResult.Succeeded)
            {
                var isDuplicate = identityResult.Errors.Any(e =>
                    e.Code is "DuplicateUserName" or "DuplicateEmail");

                return await RollbackAsync<RegisterResponse>(tx, cancellationToken,
                    isDuplicate
                        ? Error.Conflict("Identity.DuplicateEmail", "E-mail já cadastrado.")
                        : Error.Validation("Identity.InvalidUser", identityResult.Errors.First().Description));
            }

            var tenant = Tenant.Create(command.WorkspaceName, slug);
            var tenantUser = TenantUser.Create(tenant.Id, user.Id, "Owner");

            _context.Set<Tenant>().Add(tenant);
            _context.Set<TenantUser>().Add(tenantUser);
            await _context.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);

            return Result.Success(new RegisterResponse(
                user.Id,
                user.Email!,
                user.DisplayName,
                new WorkspaceDto(tenant.Name, tenant.Slug, tenant.Currency)));
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task<Result<T>> RollbackAsync<T>(
        IDbContextTransaction tx,
        CancellationToken ct,
        Error error)
    {
        await tx.RollbackAsync(ct);
        return Result.Failure<T>(error);
    }
}
