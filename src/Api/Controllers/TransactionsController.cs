using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaqFinance.Modules.Tenancy.Application.Interfaces;
using StaqFinance.Modules.Transactions.Application.Commands;
using StaqFinance.Modules.Transactions.Application.DTOs;
using StaqFinance.Modules.Transactions.Application.Queries;
using StaqFinance.Modules.Transactions.Domain.Enums;

namespace StaqFinance.Api.Controllers;

public sealed record CreateTransactionRequest(
    Guid AccountId,
    Guid? CategoryId,
    DateOnly Date,
    string Description,
    int Type,
    long AmountCents);

[ApiController]
[Route("api/workspaces/{workspaceSlug}/transactions")]
[Authorize(Policy = "MustBelongToTenant")]
public sealed class TransactionsController : ControllerBase
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ICreateTransactionCommandHandler _createHandler;
    private readonly IListTransactionsQueryHandler _listHandler;

    public TransactionsController(
        ICurrentTenant currentTenant,
        ICreateTransactionCommandHandler createHandler,
        IListTransactionsQueryHandler listHandler)
    {
        _currentTenant = currentTenant;
        _createHandler = createHandler;
        _listHandler = listHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(TransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateTransactionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.AccountId == Guid.Empty)
            return BadRequest(new { title = "accountId is required." });

        if (string.IsNullOrWhiteSpace(request.Description) || request.Description.Length > 255)
            return BadRequest(new { title = "description is required and must be at most 255 characters." });

        if (request.Type != 1 && request.Type != 2)
            return BadRequest(new { title = "type must be 1 (Income) or 2 (Expense)." });

        if (request.AmountCents <= 0)
            return BadRequest(new { title = "amountCents must be greater than 0." });

        var command = new CreateTransactionCommand(
            _currentTenant.TenantId,
            request.AccountId,
            request.CategoryId,
            request.Date,
            request.Description,
            (TransactionType)request.Type,
            request.AmountCents);

        var result = await _createHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Account.NotFound" => NotFound(new { title = result.Error.Description }),
                "Category.NotFound" => NotFound(new { title = result.Error.Description }),
                _ => BadRequest(new { title = result.Error.Description })
            };
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? accountId,
        [FromQuery] Guid? categoryId,
        [FromQuery] int? type,
        CancellationToken cancellationToken)
    {
        TransactionType? transactionType = type.HasValue ? (TransactionType)type.Value : null;

        var filter = new ListTransactionsFilter(from, to, accountId, categoryId, transactionType);
        var query = new ListTransactionsQuery(_currentTenant.TenantId, filter);
        var result = await _listHandler.HandleAsync(query, cancellationToken);

        return Ok(result.Value);
    }
}
