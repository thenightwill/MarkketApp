using Application.Common;
using Application.Interfaces.Persistance;
using Domain.Exceptions;

namespace Application.Inventory;

public class AddStockQuantityUseCase
{
    private readonly IWarehouseStockRepository _stocks;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public AddStockQuantityUseCase(
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

    public async Task<StockResponse> ExecuteAsync(Guid id, AddStockQuantityRequest request, CancellationToken cancellationToken = default)
    {
        var stock = await _stocks.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound(AppErrorCodes.InventoryNotFound, "Inventory batch not found.");

        var product = await _products.GetByIdAsync(stock.ProductId, cancellationToken);

        if (product is not null && !product.IsValidQuantity(request.Quantity))
            throw new DomainException(DomainErrorCodes.InvalidQuantity, "The quantity is not valid for the product unit type.");

        stock.AddQuantity(request.Quantity);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stock.ToResponse(product?.Name ?? string.Empty, _time.GetUtcNow().UtcDateTime);
    }
}
