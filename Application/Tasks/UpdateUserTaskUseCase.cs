using Application.Common;
using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;

namespace Application.Tasks;

// TASK MODULE (GenAI demonstration)
// The task is loaded through the repository filtered by the current user, so somebody else's task and a
// task that does not exist look identical (404). That avoids leaking which ids exist.
// The status transition is applied before the details so an illegal transition aborts the whole update.
public class UpdateUserTaskUseCase
{
    private readonly IUserTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public UpdateUserTaskUseCase(IUserTaskRepository tasks, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<UserTaskResponse> ExecuteAsync(Guid id, UpdateUserTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetByIdAsync(id, _currentUser.UserId, cancellationToken)
            ?? throw AppException.NotFound(AppErrorCodes.TaskNotFound, "Task not found.");

        task.TransitionTo(request.Status);
        task.UpdateDetails(request.Title, request.Description, request.DueDate);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return task.ToResponse();
    }
}
