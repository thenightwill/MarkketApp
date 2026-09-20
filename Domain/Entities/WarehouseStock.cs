using Domain.Exceptions;

namespace Domain.Entities;

public class WarehouseStock
{
    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public string BatchNumber { get; private set; } = null!;

    public string Location { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    public decimal MinimumStock { get; private set; }

    public DateTime ReceivedDate { get; private set; }

    public DateTime ExpirationDate { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private WarehouseStock() { }

    public static WarehouseStock Create(
        Guid productId,
        string batchNumber,
        string location,
        decimal quantity,
        decimal minimumStock,
        DateTime receivedDate,
        DateTime expirationDate)
    {
        if (productId == Guid.Empty)
            throw new DomainException(DomainErrorCodes.InvalidInventory, "Product is required.");

        if (string.IsNullOrWhiteSpace(batchNumber))
            throw new DomainException(DomainErrorCodes.InvalidInventory, "Batch number is required.");

        if (quantity <= 0)
            throw new DomainException(DomainErrorCodes.InvalidQuantity, "Quantity must be greater than zero.");

        ValidateDetails(location, minimumStock, receivedDate, expirationDate);

        return new WarehouseStock
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            BatchNumber = batchNumber.Trim(),
            Location = location.Trim(),
            Quantity = quantity,
            MinimumStock = minimumStock,
            ReceivedDate = receivedDate,
            ExpirationDate = expirationDate,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public bool IsExpired(DateTime utcNow) => ExpirationDate <= utcNow;

    public bool IsLowStock() => Quantity <= MinimumStock;

    public void AddQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException(DomainErrorCodes.InvalidQuantity, "Quantity must be greater than zero.");

        Quantity += quantity;
    }

    public void RemoveQuantity(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException(DomainErrorCodes.InvalidQuantity, "Quantity must be greater than zero.");

        if (quantity > Quantity)
            throw new DomainException(DomainErrorCodes.InsufficientStock, "Insufficient stock in the batch.");

        Quantity -= quantity;
    }

    public void UpdateDetails(string location, decimal minimumStock, DateTime expirationDate)
    {
        ValidateDetails(location, minimumStock, ReceivedDate, expirationDate);

        Location = location.Trim();
        MinimumStock = minimumStock;
        ExpirationDate = expirationDate;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    private static void ValidateDetails(
        string location,
        decimal minimumStock,
        DateTime receivedDate,
        DateTime expirationDate)
    {
        if (string.IsNullOrWhiteSpace(location))
            throw new DomainException(DomainErrorCodes.InvalidInventory, "Location is required.");

        if (minimumStock < 0)
            throw new DomainException(DomainErrorCodes.InvalidInventory, "Minimum stock cannot be negative.");

        if (expirationDate <= receivedDate)
            throw new DomainException(DomainErrorCodes.InvalidInventory, "Expiration date must be after the received date.");
    }
}
