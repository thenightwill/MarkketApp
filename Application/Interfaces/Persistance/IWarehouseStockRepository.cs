using Domain.Entities;

namespace Application.Interfaces.Persistance;

public interface IWarehouseStockRepository
{
    Task<WarehouseStock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseStock>> ListAsync(Guid? productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WarehouseStock>> GetSellableByProductIdsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default);

    Task<bool> BatchExistsAsync(
        Guid productId,
        string batchNumber,
        Guid? excludeStockId,
        CancellationToken cancellationToken = default);

    Task AddAsync(WarehouseStock stock, CancellationToken cancellationToken = default);
}
