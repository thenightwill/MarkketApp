using System.ComponentModel.DataAnnotations;
using Domain.Entities;
using Domain.Enums;

namespace Application.Tasks;

public sealed record CreateUserTaskRequest(
    [StringLength(200)] string Title,
    [StringLength(2000)] string? Description,
    DateTime DueDate);

public sealed record UpdateUserTaskRequest(
    [StringLength(200)] string Title,
    [StringLength(2000)] string? Description,
    DateTime DueDate,
    UserTaskStatus Status);

public sealed record UserTaskResponse(
    Guid Id,
    Guid UserId,
    string Title,
    string Description,
    string Status,
    DateTime DueDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);

internal static class UserTaskMapping
{
    public static UserTaskResponse ToResponse(this UserTask task) =>
        new(
            task.Id,
            task.UserId,
            task.Title,
            task.Description,
            task.Status.ToString(),
            task.DueDate,
            task.CreatedAt,
            task.UpdatedAt);
}
