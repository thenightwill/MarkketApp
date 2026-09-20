using Application.Common;
using Application.Interfaces.Persistance;

namespace Application.Inventory;

public class GetStockByIdUseCase
{
    private readonly IWarehouseStockRepository _stocks;
    private readonly IProductRepository _products;
    private readonly TimeProvider _time;

    public GetStockByIdUseCase(IWarehouseStockRepository stocks, IProductRepository products, TimeProvider time)
    {
        _stocks = stocks;
        _products = products;
        _time = time;
    }

    public async Task<StockResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var stock = await _stocks.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound(AppErrorCodes.InventoryNotFound, "Inventory batch not found.");

        var product = await _products.GetByIdAsync(stock.ProductId, cancellationToken);
        return stock.ToResponse(product?.Name ?? string.Empty, _time.GetUtcNow().UtcDateTime);
    }
}
