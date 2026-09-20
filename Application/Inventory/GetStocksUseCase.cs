using Application.Interfaces.Persistance;

namespace Application.Inventory;

public class GetStocksUseCase
{
    private readonly IWarehouseStockRepository _stocks;
    private readonly IProductRepository _products;
    private readonly TimeProvider _time;

    public GetStocksUseCase(IWarehouseStockRepository stocks, IProductRepository products, TimeProvider time)
    {
        _stocks = stocks;
        _products = products;
        _time = time;
    }

    public async Task<IReadOnlyList<StockResponse>> ExecuteAsync(StockFilter filter, CancellationToken cancellationToken = default)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var stocks = await _stocks.ListAsync(filter.ProductId, cancellationToken);

        var filtered = stocks
            .Where(s => filter.LowStock is null || s.IsLowStock() == filter.LowStock)
            .Where(s => filter.Expired is null || s.IsExpired(now) == filter.Expired)
            .ToList();

        var products = await _products.GetByIdsAsync(filtered.Select(s => s.ProductId).Distinct(), cancellationToken);
        var names = products.ToDictionary(p => p.Id, p => p.Name);

        return filtered
            .OrderBy(s => s.ExpirationDate)
            .Select(s => s.ToResponse(names.GetValueOrDefault(s.ProductId, string.Empty), now))
            .ToList();
    }
}
