using Domain.Entities;
using Domain.Enums;

namespace Application.Interfaces.Persistance;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveDuplicateAsync(
        string name,
        string brand,
        string category,
        UnitType unitType,
        decimal unitValue,
        Guid? excludeProductId,
        CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);
}
