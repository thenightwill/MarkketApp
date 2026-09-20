using Application.Common;
using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;

namespace Application.Products;

public class GetProductByIdUseCase
{
    private readonly IProductRepository _products;
    private readonly ICurrentUser _currentUser;

    public GetProductByIdUseCase(IProductRepository products, ICurrentUser currentUser)
    {
        _products = products;
        _currentUser = currentUser;
    }

    public async Task<ProductResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _products.GetByIdAsync(id, cancellationToken)
            ?? throw AppException.NotFound(AppErrorCodes.ProductNotFound, "Product not found.");

        return product.ToResponse(_currentUser.IsAdministrator);
    }
}
