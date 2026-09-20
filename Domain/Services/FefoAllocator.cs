using Domain.Entities;
using Domain.Exceptions;

namespace Domain.Services;

public sealed record StockAllocation(WarehouseStock Stock, decimal Quantity);

public static class FefoAllocator
{
    public static IReadOnlyList<StockAllocation> Allocate(
        IEnumerable<WarehouseStock> stocks,
        decimal quantity,
        DateTime utcNow)
    {
        if (quantity <= 0)
            throw new DomainException(DomainErrorCodes.InvalidQuantity, "Quantity must be greater than zero.");

        var candidates = stocks
            .Where(s => s.IsActive && s.Quantity > 0)
            .ToList();

        var usable = candidates
            .Where(s => !s.IsExpired(utcNow))
            .OrderBy(s => s.ExpirationDate)
            .ThenBy(s => s.ReceivedDate)
            .ThenBy(s => s.BatchNumber, StringComparer.Ordinal)
            .ToList();

        if (usable.Sum(s => s.Quantity) < quantity)
        {
            if (usable.Count == 0 && candidates.Count > 0)
                throw new DomainException(DomainErrorCodes.InventoryExpired, "All available stock for the product is expired.");

            throw new DomainException(DomainErrorCodes.InsufficientStock, "There is not enough stock available.");
        }

        var allocations = new List<StockAllocation>();
        var remaining = quantity;

        foreach (var stock in usable)
        {
            var taken = Math.Min(stock.Quantity, remaining);
            allocations.Add(new StockAllocation(stock, taken));
            remaining -= taken;

            if (remaining == 0)
                break;
        }

        return allocations;
    }
}
