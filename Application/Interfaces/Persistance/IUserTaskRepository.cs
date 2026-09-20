using Domain.Entities;

namespace Application.Interfaces.Persistance;

// TASK MODULE (GenAI demonstration)
// Every read method takes the owner's id. There is intentionally no "get any task by id" method, so
// application code cannot forget the ownership filter: a task that is not yours is simply not found.
public interface IUserTaskRepository
{
    Task<UserTask?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserTask>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task AddAsync(UserTask task, CancellationToken cancellationToken = default);

    void Remove(UserTask task);
}
