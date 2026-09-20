using Application.Common;
using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;
using Domain.Entities;
using Domain.Exceptions;
using Domain.Services;

namespace Application.Sales;

public class CreateSaleUseCase
{
    public const int MaxAttempts = 3;

    private readonly IProductRepository _products;
    private readonly IWarehouseStockRepository _stocks;
    private readonly ISaleRepository _sales;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly TimeProvider _time;

    public CreateSaleUseCase(
        IProductRepository products,
        IWarehouseStockRepository stocks,
        ISaleRepository sales,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        TimeProvider time)
    {
        _products = products;
        _stocks = stocks;
        _sales = sales;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _time = time;
    }

    public async Task<SaleResponse> ExecuteAsync(CreateSaleRequest request, CancellationToken cancellationToken = default)
    {
        var lines = Normalize(request);

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await _unitOfWork.ExecuteInTransactionAsync(
                    () => CreateAsync(lines, cancellationToken),
                    cancellationToken);
            }
            catch (ConcurrencyConflictException) when (attempt < MaxAttempts)
            {
            }
            catch (ConcurrencyConflictException)
            {
                throw AppException.Conflict(
                    AppErrorCodes.ConcurrencyConflict,
                    "The inventory was modified by another sale. Please try again.");
            }
        }

        throw new InvalidOperationException("Unreachable.");
    }

    private async Task<SaleResponse> CreateAsync(IReadOnlyList<CreateSaleItemRequest> lines, CancellationToken cancellationToken)
    {
        var productIds = lines.Select(l => l.ProductId).ToList();
        var products = (await _products.GetByIdsAsync(productIds, cancellationToken)).ToDictionary(p => p.Id);

        foreach (var line in lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                throw AppException.NotFound(AppErrorCodes.ProductNotFound, $"Product {line.ProductId} not found.");

            if (!product.IsActive)
                throw AppException.Conflict(AppErrorCodes.ProductInactive, $"Product {product.Name} is inactive.");

            if (!product.IsValidQuantity(line.Quantity))
                throw new DomainException(DomainErrorCodes.InvalidQuantity, $"Quantity {line.Quantity} is not valid for {product.Name}.");
        }

        var now = _time.GetUtcNow().UtcDateTime;
        var stocks = await _stocks.GetSellableByProductIdsAsync(productIds, cancellationToken);

        var plan = lines
            .Select(line => new
            {
                Product = products[line.ProductId],
                line.Quantity,
                Allocations = FefoAllocator.Allocate(stocks.Where(s => s.ProductId == line.ProductId), line.Quantity, now)
            })
            .ToList();

        var sale = Sale.Create(_currentUser.UserId, now);

        foreach (var step in plan)
        {
            foreach (var allocation in step.Allocations)
                allocation.Stock.RemoveQuantity(allocation.Quantity);

            sale.AddItem(step.Product.Id, step.Quantity, step.Product.SalePrice);
        }

        sale.Complete();

        await _sales.AddAsync(sale, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return sale.ToResponse(products.ToDictionary(p => p.Key, p => p.Value.Name));
    }

    private static IReadOnlyList<CreateSaleItemRequest> Normalize(CreateSaleRequest? request)
    {
        if (request?.Items is null || request.Items.Count == 0)
            throw new DomainException(DomainErrorCodes.SaleEmpty, "A sale must contain at least one item.");

        if (request.Items.Any(i => i.Quantity <= 0))
            throw new DomainException(DomainErrorCodes.InvalidQuantity, "Quantity must be greater than zero.");

        return request.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new CreateSaleItemRequest(g.Key, g.Sum(i => i.Quantity)))
            .ToList();
    }
}
