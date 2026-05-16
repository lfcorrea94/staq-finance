using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaqFinance.Modules.RecurringTransactions.Application.Commands;
using StaqFinance.Modules.RecurringTransactions.Application.DTOs;
using StaqFinance.Modules.RecurringTransactions.Application.Queries;
using StaqFinance.Modules.RecurringTransactions.Domain.Enums;
using StaqFinance.Modules.Tenancy.Application.Interfaces;
using StaqFinance.Modules.Transactions.Domain.Enums;

namespace StaqFinance.Api.Controllers;

public sealed record CreateRecurringTransactionRequest(
    Guid AccountId,
    Guid? CategoryId,
    string Description,
    int Type,
    long AmountCents,
    DateOnly StartDate,
    DateOnly? EndDate,
    int Frequency,
    int Interval);

[ApiController]
[Route("api/workspaces/{workspaceSlug}/recurring-transactions")]
[Authorize(Policy = "MustBelongToTenant")]
public sealed class RecurringTransactionsController : ControllerBase
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ICreateRecurringTransactionCommandHandler _createHandler;
    private readonly IRunRecurringTransactionsCommandHandler _runHandler;
    private readonly IListRecurringTransactionsQueryHandler _listHandler;

    public RecurringTransactionsController(
        ICurrentTenant currentTenant,
        ICreateRecurringTransactionCommandHandler createHandler,
        IRunRecurringTransactionsCommandHandler runHandler,
        IListRecurringTransactionsQueryHandler listHandler)
    {
        _currentTenant = currentTenant;
        _createHandler = createHandler;
        _runHandler = runHandler;
        _listHandler = listHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(RecurringTransactionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        [FromBody] CreateRecurringTransactionRequest request,
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

        if (request.StartDate == default)
            return BadRequest(new { title = "startDate is required." });

        if (request.Frequency != 1 && request.Frequency != 2)
            return BadRequest(new { title = "frequency must be 1 (Monthly) or 2 (Weekly)." });

        if (request.Interval < 1)
            return BadRequest(new { title = "interval must be at least 1." });

        if (request.EndDate.HasValue && request.EndDate.Value < request.StartDate)
            return BadRequest(new { title = "endDate must be greater than or equal to startDate.", code = "EndDate.BeforeStart" });

        var command = new CreateRecurringTransactionCommand(
            _currentTenant.TenantId,
            request.AccountId,
            request.CategoryId,
            request.Description,
            (TransactionType)request.Type,
            request.AmountCents,
            request.StartDate,
            request.EndDate,
            (RecurringFrequency)request.Frequency,
            request.Interval);

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
    [ProducesResponseType(typeof(IReadOnlyList<RecurringTransactionResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var filter = new ListRecurringTransactionsFilter(isActive);
        var query = new ListRecurringTransactionsQuery(_currentTenant.TenantId, filter);
        var result = await _listHandler.HandleAsync(query, cancellationToken);
        return Ok(result.Value);
    }

    [HttpPost("run")]
    [ProducesResponseType(typeof(RunRecurringTransactionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Run(
        [FromQuery] DateOnly? until,
        CancellationToken cancellationToken)
    {
        if (until is null)
            return BadRequest(new { title = "until is required.", code = "Until.Required" });

        var command = new RunRecurringTransactionsCommand(_currentTenant.TenantId, until.Value);
        var result = await _runHandler.HandleAsync(command, cancellationToken);
        return Ok(result.Value);
    }
}
