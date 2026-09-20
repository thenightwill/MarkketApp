using System.ComponentModel.DataAnnotations;
using Domain.Entities;
using Domain.Enums;

namespace Application.Products;

public sealed record CreateProductRequest(
    [StringLength(200)] string Name,
    [StringLength(100)] string Brand,
    [StringLength(100)] string Category,
    UnitType UnitType,
    decimal UnitValue,
    decimal Cost,
    decimal SalePrice);

public sealed record UpdateProductRequest(
    [StringLength(200)] string Name,
    [StringLength(100)] string Brand,
    [StringLength(100)] string Category,
    UnitType UnitType,
    decimal UnitValue,
    decimal Cost,
    decimal SalePrice,
    bool IsActive);

public sealed record ProductResponse(
    Guid Id,
    string Name,
    string Brand,
    string Category,
    string UnitType,
    decimal UnitValue,
    decimal? Cost,
    decimal SalePrice,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt);

internal static class ProductMapping
{
    public static ProductResponse ToResponse(this Product product, bool includeCost) =>
        new(
            product.Id,
            product.Name,
            product.Brand,
            product.Category,
            product.UnitType.ToString(),
            product.UnitValue,
            includeCost ? product.Cost : null,
            product.SalePrice,
            product.IsActive,
            product.CreatedAt,
            product.UpdatedAt);
}
