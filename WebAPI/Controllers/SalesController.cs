using Application.Sales;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    [HttpPost]
    [EndpointSummary("Create a sale")]
    [EndpointDescription("The request only carries productId and quantity per item. The backend resolves the user (from the JWT), the unit price and the batches consumed with FEFO, inside a single transaction.")]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SaleResponse>> Create(
        [FromBody] CreateSaleRequest request,
        [FromServices] CreateSaleUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet]
    [EndpointSummary("List sales")]
    [EndpointDescription("Employees see only their own sales; Administrators see every sale.")]
    [ProducesResponseType<IReadOnlyList<SaleResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SaleResponse>>> GetAll(
        [FromServices] GetSalesUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get a sale by id")]
    [EndpointDescription("Returns 404 if the sale belongs to a different employee; Administrators can read any sale.")]
    [ProducesResponseType<SaleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SaleResponse>> GetById(
        Guid id,
        [FromServices] GetSaleByIdUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(id, cancellationToken));
}
