using Application.Interfaces.Persistance;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Persistance.Repositories;

public class WarehouseStockRepository : IWarehouseStockRepository
{
    private readonly AppDbContext _context;

    public WarehouseStockRepository(AppDbContext context) => _context = context;

    public Task<WarehouseStock?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.WarehouseStocks.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<WarehouseStock>> ListAsync(Guid? productId, CancellationToken cancellationToken = default) =>
        await _context.WarehouseStocks
            .AsNoTracking()
            .Where(s => productId == null || s.ProductId == productId)
            .OrderBy(s => s.ExpirationDate)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WarehouseStock>> GetSellableByProductIdsAsync(
        IEnumerable<Guid> productIds,
        CancellationToken cancellationToken = default)
    {
        var ids = productIds.Distinct().ToList();
        return await _context.WarehouseStocks
            .Where(s => ids.Contains(s.ProductId) && s.IsActive && s.Quantity > 0)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> BatchExistsAsync(
        Guid productId,
        string batchNumber,
        Guid? excludeStockId,
        CancellationToken cancellationToken = default) =>
        _context.WarehouseStocks.AnyAsync(
            s => s.ProductId == productId && s.BatchNumber == batchNumber && s.Id != excludeStockId,
            cancellationToken);

    public async Task AddAsync(WarehouseStock stock, CancellationToken cancellationToken = default) =>
        await _context.WarehouseStocks.AddAsync(stock, cancellationToken);
}
