using System.ComponentModel.DataAnnotations;
using Domain.Entities;

namespace Application.Inventory;

public sealed record CreateStockRequest(
    Guid ProductId,
    [StringLength(50)] string BatchNumber,
    [StringLength(50)] string Location,
    decimal Quantity,
    decimal MinimumStock,
    DateTime ReceivedDate,
    DateTime ExpirationDate);

public sealed record UpdateStockRequest(
    [StringLength(50)] string Location,
    decimal MinimumStock,
    DateTime ExpirationDate,
    bool IsActive);

public sealed record AddStockQuantityRequest(decimal Quantity);

public sealed record StockFilter(Guid? ProductId, bool? LowStock, bool? Expired);

public sealed record StockResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    [StringLength(50)] string BatchNumber,
    [StringLength(50)] string Location,
    decimal Quantity,
    decimal MinimumStock,
    DateTime ReceivedDate,
    DateTime ExpirationDate,
    bool IsActive,
    bool IsLowStock,
    bool IsExpired,
    DateTime CreatedAt);

internal static class StockMapping
{
    public static StockResponse ToResponse(this WarehouseStock stock, string productName, DateTime utcNow) =>
        new(
            stock.Id,
            stock.ProductId,
            productName,
            stock.BatchNumber,
            stock.Location,
            stock.Quantity,
            stock.MinimumStock,
            stock.ReceivedDate,
            stock.ExpirationDate,
            stock.IsActive,
            stock.IsLowStock(),
            stock.IsExpired(utcNow),
            stock.CreatedAt);
}
