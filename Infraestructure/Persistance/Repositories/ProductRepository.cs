using Application.Interfaces.Persistance;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Persistance.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context) => _context = context;

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var list = ids.Distinct().ToList();
        return await _context.Products.Where(p => list.Contains(p.Id)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default) =>
        await _context.Products
            .AsNoTracking()
            .Where(p => includeInactive || p.IsActive)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Brand)
            .ToListAsync(cancellationToken);

    public Task<bool> ExistsActiveDuplicateAsync(
        string name,
        string brand,
        string category,
        UnitType unitType,
        decimal unitValue,
        Guid? excludeProductId,
        CancellationToken cancellationToken = default) =>
        _context.Products.AnyAsync(
            p => p.IsActive
                && p.Id != excludeProductId
                && p.Name == name
                && p.Brand == brand
                && p.Category == category
                && p.UnitType == unitType
                && p.UnitValue == unitValue,
            cancellationToken);

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default) =>
        await _context.Products.AddAsync(product, cancellationToken);
}
