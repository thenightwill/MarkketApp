using Application.Common;
using Application.Interfaces.Persistance;

namespace Application.Products;

public class UpdateProductUseCase
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateProductUseCase(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductResponse> ExecuteAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound(AppErrorCodes.ProductNotFound, "Product not found.");

        product.Update(
            request.Name,
            request.Brand,
            request.Category,
            request.UnitType,
            request.UnitValue,
            request.Cost,
            request.SalePrice);

        if (request.IsActive)
        {
            var duplicated = await _products.ExistsActiveDuplicateAsync(
                product.Name,
                product.Brand,
                product.Category,
                product.UnitType,
                product.UnitValue,
                product.Id,
                cancellationToken);

            if (duplicated)
                throw AppException.Conflict(AppErrorCodes.DuplicateProduct, "An active product with the same identity already exists.");

            if (!product.IsActive)
                product.Activate();
        }
        else if (product.IsActive)
        {
            product.Deactivate();
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToResponse(includeCost: true);
    }
}
