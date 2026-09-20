using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

// TASK MODULE (GenAI demonstration)
// The aggregate is named UserTask instead of Task because, with ImplicitUsings enabled, a type called
// Task (or an enum called TaskStatus) silently collides with System.Threading.Tasks. The original stub in
// this repository compiled only because TaskStatus resolved to the BCL enum, not to our own status enum.
// The database table, the HTTP routes and the UI keep the business name "tasks".
public class UserTask
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public string Title { get; private set; } = null!;

    public string Description { get; private set; } = string.Empty;

    public UserTaskStatus Status { get; private set; }

    public DateTime DueDate { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    private UserTask() { }

    public static UserTask Create(Guid userId, string title, string? description, DateTime dueDate)
    {
        if (userId == Guid.Empty)
            throw new DomainException(DomainErrorCodes.InvalidTask, "A task must belong to a user.");

        ValidateTitle(title);

        return new UserTask
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Description = description?.Trim() ?? string.Empty,
            Status = UserTaskStatus.Pending,
            DueDate = dueDate,
            CreatedAt = DateTime.UtcNow
        };
    }

    // The aggregate is the only place that knows the state machine:
    // Pending -> InProgress -> Completed. Nothing can skip a step or move backwards.
    public void Start() => Move(UserTaskStatus.Pending, UserTaskStatus.InProgress);

    public void Complete() => Move(UserTaskStatus.InProgress, UserTaskStatus.Completed);

    // Used by the PUT endpoint, which receives the desired status. Asking for the current status is a no-op
    // so that clients can resend the whole form without triggering a false transition error.
    public void TransitionTo(UserTaskStatus target)
    {
        if (target == Status)
            return;

        switch (target)
        {
            case UserTaskStatus.InProgress:
                Start();
                break;
            case UserTaskStatus.Completed:
                Complete();
                break;
            default:
                throw InvalidTransition(target);
        }
    }

    // Validation happens before any assignment so an invalid update never leaves the task half modified.
    public void UpdateDetails(string title, string? description, DateTime dueDate)
    {
        ValidateTitle(title);

        Title = title.Trim();
        Description = description?.Trim() ?? string.Empty;
        DueDate = dueDate;
        UpdatedAt = DateTime.UtcNow;
    }

    // Ownership is a domain rule, not only an HTTP concern: the application layer relies on this to make
    // sure a user can never read or change tasks that belong to someone else.
    public bool IsOwnedBy(Guid userId) => UserId == userId;

    private void Move(UserTaskStatus expectedCurrent, UserTaskStatus target)
    {
        if (Status != expectedCurrent)
            throw InvalidTransition(target);

        Status = target;
        UpdatedAt = DateTime.UtcNow;
    }

    private DomainException InvalidTransition(UserTaskStatus target) =>
        new(DomainErrorCodes.InvalidTaskTransition, $"A task cannot move from {Status} to {target}.");

    private static void ValidateTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException(DomainErrorCodes.InvalidTask, "Task title is required.");
    }
}
