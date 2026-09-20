using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;
using Domain.Entities;
using Domain.Enums;

namespace Market.Tests.Application.Fakes;

public sealed class FakeTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _now;

    public FakeTimeProvider(DateTime utcNow) => _now = new DateTimeOffset(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc));

    public override DateTimeOffset GetUtcNow() => _now;
}

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid UserId { get; set; } = Guid.NewGuid();

    public UserRole Role { get; set; } = UserRole.Employee;
}

public sealed class FakeUnitOfWork : IUnitOfWork
{
    public int SaveCount { get; private set; }

    public int TransactionCount { get; private set; }

    public int ConflictsToRaise { get; set; }

    public Action? OnConflict { get; set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ConflictsToRaise > 0)
        {
            ConflictsToRaise--;
            OnConflict?.Invoke();
            throw new ConcurrencyConflictException("Simulated concurrency conflict.");
        }

        SaveCount++;
        return Task.CompletedTask;
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
    {
        TransactionCount++;
        return await action();
    }
}

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => $"hashed::{password}";

    public bool Verify(string password, string passwordHash) => passwordHash == Hash(password);
}

public sealed class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public AccessToken Generate(User user) =>
        new($"token-for-{user.Id}", new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
}

public sealed class InMemoryProductRepository : IProductRepository
{
    public List<Product> Items { get; } = new();

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var set = ids.ToHashSet();
        return Task.FromResult<IReadOnlyList<Product>>(Items.Where(p => set.Contains(p.Id)).ToList());
    }

    public Task<IReadOnlyList<Product>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>(Items.Where(p => includeInactive || p.IsActive).OrderBy(p => p.Name).ToList());

    public Task<bool> ExistsActiveDuplicateAsync(
        string name,
        string brand,
        string category,
        UnitType unitType,
        decimal unitValue,
        Guid? excludeProductId,
        CancellationToken cancellationToken = default)
    {
        var exists = Items.Any(p =>
            p.IsActive
            && p.Id != excludeProductId
            && string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.Brand, brand.Trim(), StringComparison.OrdinalIgnoreCase)
            && string.Equals(p.Category, category.Trim(), StringComparison.OrdinalIgnoreCase)
            && p.UnitType == unitType
            && p.UnitValue == unitValue);

        return Task.FromResult(exists);
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        Items.Add(product);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryWarehouseStockRepository : IWarehouseStockRepository
{
    public List<WarehouseStock> Items { get; } = new();

    public Task<WarehouseStock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<WarehouseStock>> ListAsync(Guid? productId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<WarehouseStock>>(
            Items.Where(s => productId is null || s.ProductId == productId).ToList());

    public Task<IReadOnlyList<WarehouseStock>> GetSellableByProductIdsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var set = productIds.ToHashSet();
        return Task.FromResult<IReadOnlyList<WarehouseStock>>(
            Items.Where(s => set.Contains(s.ProductId) && s.IsActive && s.Quantity > 0).ToList());
    }

    public Task<bool> BatchExistsAsync(
        Guid productId,
        string batchNumber,
        Guid? excludeStockId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(s =>
            s.ProductId == productId
            && s.Id != excludeStockId
            && string.Equals(s.BatchNumber, batchNumber.Trim(), StringComparison.OrdinalIgnoreCase)));

    public Task AddAsync(WarehouseStock stock, CancellationToken cancellationToken = default)
    {
        Items.Add(stock);
        return Task.CompletedTask;
    }
}

public sealed class InMemorySaleRepository : ISaleRepository
{
    public List<Sale> Items { get; } = new();

    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(s => s.Id == id));

    public Task<IReadOnlyList<Sale>> ListAsync(Guid? userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Sale>>(
            Items.Where(s => userId is null || s.UserId == userId).OrderByDescending(s => s.SaleDate).ToList());

    public Task AddAsync(Sale sale, CancellationToken cancellationToken = default)
    {
        Items.Add(sale);
        return Task.CompletedTask;
    }
}

public sealed class InMemoryUserTaskRepository : IUserTaskRepository
{
    public List<UserTask> Items { get; } = new();

    public Task<UserTask?> GetByIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(t => t.Id == id && t.UserId == userId));

    public Task<IReadOnlyList<UserTask>> ListByUserAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<UserTask>>(Items.Where(t => t.UserId == userId).OrderBy(t => t.DueDate).ToList());

    public Task AddAsync(UserTask task, CancellationToken cancellationToken = default)
    {
        Items.Add(task);
        return Task.CompletedTask;
    }

    public void Remove(UserTask task) => Items.Remove(task);
}

public sealed class InMemoryUserRepository : IUserRepository
{
    public List<User> Items { get; } = new();

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant()));

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Items.Any(u => u.Email == email.Trim().ToLowerInvariant()));

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        Items.Add(user);
        return Task.CompletedTask;
    }
}
