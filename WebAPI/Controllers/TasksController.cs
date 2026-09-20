using Application.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Controllers;

// TASK MODULE (GenAI demonstration)
// The controller stays deliberately thin: it never receives or forwards a user id. Ownership is resolved
// inside the use cases from the JWT, so there is no parameter here that a client could tamper with.
[ApiController]
[Authorize]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    [HttpGet]
    [EndpointSummary("List my tasks")]
    [EndpointDescription("Always scoped to the caller from the JWT. Administrators only see their own tasks too.")]
    [ProducesResponseType<IReadOnlyList<UserTaskResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserTaskResponse>>> GetAll(
        [FromServices] GetUserTasksUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [EndpointSummary("Get one of my tasks by id")]
    [EndpointDescription("Returns 404 for a task that does not exist and for a task owned by someone else, so an id cannot be used to probe ownership.")]
    [ProducesResponseType<UserTaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserTaskResponse>> GetById(
        Guid id,
        [FromServices] GetUserTaskByIdUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(id, cancellationToken));

    [HttpPost]
    [EndpointSummary("Create a task")]
    [EndpointDescription("Owned by the caller from the JWT; any userId sent in the body is ignored. Starts as Pending.")]
    [ProducesResponseType<UserTaskResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UserTaskResponse>> Create(
        [FromBody] CreateUserTaskRequest request,
        [FromServices] CreateUserTaskUseCase useCase,
        CancellationToken cancellationToken)
    {
        var response = await useCase.ExecuteAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpPut("{id:guid}")]
    [EndpointSummary("Update a task")]
    [EndpointDescription("Updates title, description and due date, and applies the requested status transition. Only Pending -> InProgress -> Completed is allowed.")]
    [ProducesResponseType<UserTaskResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserTaskResponse>> Update(
        Guid id,
        [FromBody] UpdateUserTaskRequest request,
        [FromServices] UpdateUserTaskUseCase useCase,
        CancellationToken cancellationToken) =>
        Ok(await useCase.ExecuteAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [EndpointSummary("Delete a task")]
    [EndpointDescription("Physical delete: tasks are personal notes, not business records referenced elsewhere.")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] DeleteUserTaskUseCase useCase,
        CancellationToken cancellationToken)
    {
        await useCase.ExecuteAsync(id, cancellationToken);
        return NoContent();
    }
}
