using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;

namespace Application.Products;

public class GetProductsUseCase
{
    private readonly IProductRepository _products;
    private readonly ICurrentUser _currentUser;

    public GetProductsUseCase(IProductRepository products, ICurrentUser currentUser)
    {
        _products = products;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<ProductResponse>> ExecuteAsync(bool includeInactive, CancellationToken cancellationToken = default)
    {
        var items = await _products.ListAsync(includeInactive, cancellationToken);
        return items.Select(p => p.ToResponse(_currentUser.IsAdministrator)).ToList();
    }
}
