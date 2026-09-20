using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;
using Domain.Entities;

namespace Application.Tasks;

// TASK MODULE (GenAI demonstration)
// The owner is never read from the request body: it is taken from ICurrentUser, which is fed by the JWT.
// A client that sends a "userId" field simply has it ignored because the DTO does not declare it.
public class CreateUserTaskUseCase
{
    private readonly IUserTaskRepository _tasks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;

    public CreateUserTaskUseCase(IUserTaskRepository tasks, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        _tasks = tasks;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<UserTaskResponse> ExecuteAsync(CreateUserTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = UserTask.Create(_currentUser.UserId, request.Title, request.Description, request.DueDate);

        await _tasks.AddAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return task.ToResponse();
    }
}
