using Application.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory")]
public class InventoryController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List inventory batches")]
    [EndpointDescription("Optional filters: productId, lowStock (Quantity <= MinimumStock) and expired (ExpirationDate <= now).")]
    [ProducesResponseType<IReadOnlyList<StockResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StockResponse>>> GetAll(
        [FromServices] GetStocksUseCase useCase,
        [FromQuery] Guid? productId,
        [FromQuery] bool? lowStock,
        [FromQuery] bool? expired,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(new StockFilter(productId, lowStock, expired), cancellationToken));

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get an inventory batch by id")]
    [ProducesResponseType<StockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockResponse>> GetById(
        Guid id,
        [FromServices] GetStockByIdUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthenticationExtensions.AdministratorPolicy)]
    [EndpointSummary("Register a new batch")]
    [EndpointDescription("Administrator only. The product must exist and be active. BatchNumber must be unique for the product.")]
    [ProducesResponseType<StockResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StockResponse>> Create(
        [FromBody] CreateStockRequest request,
        [FromServices] CreateStockUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthenticationExtensions.AdministratorPolicy)]
    [EndpointSummary("Update batch details")]
    [EndpointDescription("Administrator only. Updates Location, MinimumStock, ExpirationDate and IsActive. Never changes Quantity; use add-quantity or a sale for that.")]
    [ProducesResponseType<StockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockResponse>> Update(
        Guid id,
        [FromBody] UpdateStockRequest request,
        [FromServices] UpdateStockUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/add-quantity")]
    [Authorize(Policy = AuthenticationExtensions.AdministratorPolicy)]
    [EndpointSummary("Restock a batch")]
    [EndpointDescription("Administrator only. Adds the given quantity to the batch. The only endpoint that increases Quantity; sales are the only way to decrease it.")]
    [ProducesResponseType<StockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StockResponse>> AddQuantity(
        Guid id,
        [FromBody] AddStockQuantityRequest request,
        [FromServices] AddStockQuantityUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(id, request, cancellationToken));
}
