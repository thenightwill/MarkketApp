using Application.Common;
using Application.Interfaces.Persistance;

namespace Application.Products;

public class DeactivateProductUseCase
{
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateProductUseCase(IProductRepository products, IUnitOfWork unitOfWork)
    {
        _products = products;
        _unitOfWork = unitOfWork;
    }

    public async Task ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound(AppErrorCodes.ProductNotFound, "Product not found.");

        if (product.IsActive)
        {
            product.Deactivate();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
