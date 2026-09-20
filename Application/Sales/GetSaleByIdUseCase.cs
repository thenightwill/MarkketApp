using Application.Common;
using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;

namespace Application.Sales;

public class GetSaleByIdUseCase
{
    private readonly ISaleRepository _sales;
    private readonly IProductRepository _products;
    private readonly ICurrentUser _currentUser;

    public GetSaleByIdUseCase(ISaleRepository sales, IProductRepository products, ICurrentUser currentUser)
    {
        _sales = sales;
        _products = products;
        _currentUser = currentUser;
    }

    public async Task<SaleResponse> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var sale = await _sales.GetByIdAsync(id, cancellationToken);

        if (sale is null || (!_currentUser.IsAdministrator && sale.UserId != _currentUser.UserId))
            throw AppException.NotFound(AppErrorCodes.SaleNotFound, "Sale not found.");

        var products = await _products.GetByIdsAsync(sale.Items.Select(i => i.ProductId).Distinct(), cancellationToken);
        return sale.ToResponse(products.ToDictionary(p => p.Id, p => p.Name));
    }
}
