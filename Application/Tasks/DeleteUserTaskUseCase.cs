using Application.Common;
using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;

namespace Application.Tasks;

// TASK MODULE (GenAI demonstration)
// Unlike products, tasks are physically deleted: they are personal notes, not business records that
// other aggregates reference.
public class DeleteUserTaskUseCase
{
    private readonly IUserTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public DeleteUserTaskUseCase(IUserTaskRepository tasks, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetByIdAsync(id, _currentUser.UserId, cancellationToken)
            ?? throw AppException.NotFound(AppErrorCodes.TaskNotFound, "Task not found.");

        _tasks.Remove(task);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
