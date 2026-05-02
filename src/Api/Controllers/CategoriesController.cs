using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StaqFinance.Modules.Categories.Application.Commands;
using StaqFinance.Modules.Categories.Application.DTOs;
using StaqFinance.Modules.Categories.Application.Queries;
using StaqFinance.Modules.Tenancy.Application.Interfaces;

namespace StaqFinance.Api.Controllers;

[ApiController]
[Route("api/workspaces/{workspaceSlug}/categories")]
[Authorize(Policy = "MustBelongToTenant")]
public sealed class CategoriesController : ControllerBase
{
    private readonly ICurrentTenant _currentTenant;
    private readonly ICreateCategoryCommandHandler _createHandler;
    private readonly IListCategoriesQueryHandler _listHandler;

    public CategoriesController(
        ICurrentTenant currentTenant,
        ICreateCategoryCommandHandler createHandler,
        IListCategoriesQueryHandler listHandler)
    {
        _currentTenant = currentTenant;
        _createHandler = createHandler;
        _listHandler = listHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CategoryResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            return BadRequest(new { title = "Name is required and must be at most 100 characters." });

        var command = new CreateCategoryCommand(_currentTenant.TenantId, request.Name);
        var result = await _createHandler.HandleAsync(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.Code switch
            {
                "Category.DuplicateName" => Conflict(new { title = result.Error.Description }),
                _ => BadRequest(new { title = result.Error.Description })
            };
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var query = new ListCategoriesQuery(_currentTenant.TenantId);
        var result = await _listHandler.HandleAsync(query, cancellationToken);

        return Ok(result.Value);
    }
}
