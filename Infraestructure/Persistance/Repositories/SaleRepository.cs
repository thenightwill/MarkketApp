using Application.Interfaces.Persistance;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Persistance.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _context;

    public SaleRepository(AppDbContext context) => _context = context;

    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Sales.Include(s => s.Items).FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Sale>> ListAsync(Guid? userId, CancellationToken cancellationToken = default) =>
        await _context.Sales
            .AsNoTracking()
            .Include(s => s.Items)
            .Where(s => userId == null || s.UserId == userId)
            .OrderByDescending(s => s.SaleDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Sale sale, CancellationToken cancellationToken = default) =>
        await _context.Sales.AddAsync(sale, cancellationToken);
}
