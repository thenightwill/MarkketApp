using Domain.Entities;

namespace Application.Sales;

public sealed record CreateSaleItemRequest(Guid ProductId, decimal Quantity);

public sealed record CreateSaleRequest(IReadOnlyList<CreateSaleItemRequest> Items);

public sealed record SaleItemResponse(
    Guid ProductId,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal Subtotal);

public sealed record SaleResponse(
    Guid Id,
    Guid UserId,
    DateTime SaleDate,
    decimal Total,
    string Status,
    IReadOnlyList<SaleItemResponse> Items);

internal static class SaleMapping
{
    public static SaleResponse ToResponse(this Sale sale, IReadOnlyDictionary<Guid, string> productNames) =>
        new(
            sale.Id,
            sale.UserId,
            sale.SaleDate,
            sale.Total,
            sale.Status.ToString(),
            sale.Items
                .Select(i => new SaleItemResponse(
                    i.ProductId,
                    productNames.GetValueOrDefault(i.ProductId, string.Empty),
                    i.Quantity,
                    i.UnitPrice,
                    i.Subtotal))
                .ToList());
}
