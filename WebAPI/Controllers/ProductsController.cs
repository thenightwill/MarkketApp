using Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Extensions;
using WebAPI.Security;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List products")]
    [EndpointDescription("Active products only, unless includeInactive=true and the caller is an Administrator. Cost is only included for Administrators.")]
    [ProducesResponseType<IReadOnlyList<ProductResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll(
        [FromServices] GetProductsUseCase useCase,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(includeInactive && User.IsInRole(Roles.Administrator), cancellationToken));

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a product by id")]
    [EndpointDescription("Cost is only included for Administrators.")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductResponse>> GetById(
        Guid id,
        [FromServices] GetProductByIdUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = AuthenticationExtensions.AdministratorPolicy)]
    [EndpointSummary("Create a product")]
    [EndpointDescription("Administrator only. Rejects an active duplicate with the same Name, Brand, Category, UnitType and UnitValue.")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Create(
        [FromBody] CreateProductRequest request,
        [FromServices] CreateProductUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthenticationExtensions.AdministratorPolicy)]
    [EndpointSummary("Update a product")]
    [EndpointDescription("Administrator only. isActive can reactivate or deactivate the product; reactivating still checks for active duplicates.")]
    [ProducesResponseType<ProductResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductResponse>> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        [FromServices] UpdateProductUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthenticationExtensions.AdministratorPolicy)]
    [EndpointSummary("Deactivate a product")]
    [EndpointDescription("Administrator only. Logical delete: the product row is kept and IsActive is set to false.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(
        Guid id,
        [FromServices] DeactivateProductUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
