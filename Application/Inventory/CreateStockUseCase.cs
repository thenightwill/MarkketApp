using Application.Common;
using Application.Interfaces.Persistance;
using Domain.Entities;
using Domain.Exceptions;

namespace Application.Inventory;

public class CreateStockUseCase
{
    private readonly IProductRepository _products;
    private readonly IWarehouseStockRepository _stocks;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _time;

    public CreateStockUseCase(
        IProductRepository products,
        IWarehouseStockRepository stocks,
        IUnitOfWork unitOfWork,
        TimeProvider time)
    {
        _products = products;
        _stocks = stocks;
        _unitOfWork = unitOfWork;
        _time = time;
    }

    public async Task<StockResponse> ExecuteAsync(CreateStockRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(request.ProductId, cancellationToken)
            ?? throw AppException.NotFound(AppErrorCodes.ProductNotFound, "Product not found.");

        if (!product.IsActive)
            throw AppException.Conflict(AppErrorCodes.ProductInactive, "The product is inactive.");

        if (!product.IsValidQuantity(request.Quantity))
            throw new DomainException(DomainErrorCodes.InvalidQuantity, "The quantity is not valid for the product unit type.");

        var stock = WarehouseStock.Create(
            request.ProductId,
            request.BatchNumber,
            request.Location,
            request.Quantity,
            request.MinimumStock,
            request.ReceivedDate,
            request.ExpirationDate);

        if (await _stocks.BatchExistsAsync(stock.ProductId, stock.BatchNumber, null, cancellationToken))
            throw AppException.Conflict(AppErrorCodes.DuplicateBatch, "The batch number already exists for the product.");

        await _stocks.AddAsync(stock, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return stock.ToResponse(product.Name, _time.GetUtcNow().UtcDateTime);
    }
}
