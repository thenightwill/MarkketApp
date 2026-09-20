using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;

namespace Application.Tasks;

// TASK MODULE (GenAI demonstration)
// Administrators get no special treatment here: the requirement is that a user never sees other users' tasks.
public class GetUserTasksUseCase
{
    private readonly IUserTaskRepository _tasks;
    private readonly ICurrentUser _currentUser;

    public GetUserTasksUseCase(IUserTaskRepository tasks, ICurrentUser currentUser)
    {
        _tasks = tasks;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<UserTaskResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _tasks.ListByUserAsync(_currentUser.UserId, cancellationToken);
        return tasks.Select(t => t.ToResponse()).ToList();
    }
}
