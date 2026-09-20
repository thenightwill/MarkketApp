using Application.Interfaces.Persistance;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Persistance.Repositories;

// TASK MODULE (GenAI demonstration)
// The owner filter lives inside the query, not after loading, so a row that belongs to another user is
// never even materialized.
public class UserTaskRepository : IUserTaskRepository
{
    private readonly AppDbContext _context;

    public UserTaskRepository(AppDbContext context) => _context = context;

    public Task<UserTask?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        _context.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId, cancellationToken);

    public async Task<IReadOnlyList<UserTask>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await _context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.DueDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(UserTask task, CancellationToken cancellationToken = default) =>
        await _context.Tasks.AddAsync(task, cancellationToken);

    public void Remove(UserTask task) => _context.Tasks.Remove(task);
}
