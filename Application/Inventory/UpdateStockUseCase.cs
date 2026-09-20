using Application.Common;
using Application.Interfaces.Persistance;

namespace Application.Inventory;

public class UpdateStockUseCase
{
    private readonly IWarehouseStockRepository _stocks;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public UpdateStockUseCase(
        IWarehouseStockRepository stocks,
        IProductRepository products,
        IUnitOfWork unitOfWork,
        TimeProvider time)
    {
        _stocks = stocks;
        _products = products;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<StockResponse> ExecuteAsync(Guid id, UpdateStockRequest request, CancellationToken cancellationToken = default)
    {
        var stock = await _stocks.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound(AppErrorCodes.InventoryNotFound, "Inventory batch not found.");

        stock.UpdateDetails(request.Location, request.MinimumStock, request.ExpirationDate);

        if (request.IsActive)
            stock.Activate();
        else
            stock.Deactivate();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var product = await _products.GetByIdAsync(stock.ProductId, cancellationToken);
        return stock.ToResponse(product?.Name ?? string.Empty, _time.GetUtcNow().UtcDateTime);
    }
}
