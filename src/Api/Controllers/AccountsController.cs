using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaqFinance.Modules.Accounts.Application.Commands;
using StaqFinance.Modules.Accounts.Application.DTOs;
using StaqFinance.Modules.Accounts.Application.Queries;
using StaqFinance.Modules.Tenancy.Application.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace StaqFinance.Api.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceSlug}/accounts")]
[Authorize(Policy = "MustBelongToTenant")]
public sealed class AccountsController : ControllerBase
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ICreateAccountCommandHandler _createHandler;
    private readonly IListAccountsQueryHandler _listHandler;

    public AccountsController(
        ICurrentTenant currentTenant,
        ICreateAccountCommandHandler createHandler,
        IListAccountsQueryHandler listHandler)
    {
        _currentTenant = currentTenant;
        _createHandler = createHandler;
        _listHandler = listHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AccountResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            return BadRequest(new { title = "Name is required and must be at most 100 characters." });

        var command = new CreateAccountCommand(_currentTenant.TenantId, request.Name);
        var result = await _createHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Account.DuplicateName" => Conflict(new { title = result.Error.Description }),
                _ => BadRequest(new { title = result.Error.Description })
            };
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccountResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var query = new ListAccountsQuery(_currentTenant.TenantId);
        var result = await _listHandler.HandleAsync(query, cancellationToken);

        return Ok(result.Value);
    }
}
