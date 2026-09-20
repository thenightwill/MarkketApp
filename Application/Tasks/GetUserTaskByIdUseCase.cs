using Application.Common;
using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;

namespace Application.Tasks;

// TASK MODULE (GenAI demonstration)
public class GetUserTaskByIdUseCase
{
    private readonly IUserTaskRepository _tasks;
    private readonly ICurrentUser _currentUser;

    public GetUserTaskByIdUseCase(IUserTaskRepository tasks, ICurrentUser currentUser)
    {
        _tasks = tasks;
        _currentUser = currentUser;
    }

    public async Task<UserTaskResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetByIdAsync(id, _currentUser.UserId, cancellationToken)
            ?? throw AppException.NotFound(AppErrorCodes.TaskNotFound, "Task not found.");

        return task.ToResponse();
    }
}
