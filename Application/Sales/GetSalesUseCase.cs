using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;

namespace Application.Sales;

public class GetSalesUseCase
{
    private readonly ISaleRepository _sales;
    private readonly IProductRepository _products;
    private readonly ICurrentUser _currentUser;

    public GetSalesUseCase(ISaleRepository sales, IProductRepository products, ICurrentUser currentUser)
    {
        _sales = sales;
        _products = products;
        _currentUser = currentUser;
    }

    public async Task<IReadOnlyList<SaleResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var scope = _currentUser.IsAdministrator ? (Guid?)null : _currentUser.UserId;
        var sales = await _sales.ListAsync(scope, cancellationToken);

        var productIds = sales.SelectMany(s => s.Items.Select(i => i.ProductId)).Distinct();
        var products = await _products.GetByIdsAsync(productIds, cancellationToken);
        var names = products.ToDictionary(p => p.Id, p => p.Name);

        return sales.Select(s => s.ToResponse(names)).ToList();
    }
}
