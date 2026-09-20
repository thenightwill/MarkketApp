using Application.Common;
using Application.Interfaces.Persistance;
using Domain.Entities;

namespace Application.Products;

public class CreateProductUseCase
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductUseCase(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductResponse> ExecuteAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = Product.Create(
            request.Name,
            request.Brand,
            request.Category,
            request.UnitType,
            request.UnitValue,
            request.Cost,
            request.SalePrice);

        var duplicated = await _products.ExistsActiveDuplicateAsync(
            product.Name,
            product.Brand,
            product.Category,
            product.UnitType,
            product.UnitValue,
            null,
            cancellationToken);

        if (duplicated)
            throw AppException.Conflict(AppErrorCodes.DuplicateProduct, "An active product with the same identity already exists.");

        await _products.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToResponse(includeCost: true);
    }
}
