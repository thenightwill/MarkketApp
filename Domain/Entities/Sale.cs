using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class Sale
{
    private readonly List<SaleItem> _items = new();

    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }

    public DateTime SaleDate { get; private set; }

    public decimal Total { get; private set; }

    public SaleStatus Status { get; private set; }

    public IReadOnlyCollection<SaleItem> Items => _items.AsReadOnly();

    private Sale() { }

    public static Sale Create(Guid userId, DateTime saleDate)
    {
        if (userId == Guid.Empty)
            throw new DomainException(DomainErrorCodes.InvalidSale, "User is required.");

        return new Sale
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SaleDate = saleDate,
            Status = SaleStatus.Incomplete,
            Total = 0m
        };
    }

    public void AddItem(Guid productId, decimal quantity, decimal unitPrice)
    {
        EnsureNotCompleted();

        if (_items.Any(i => i.ProductId == productId))
            throw new DomainException(DomainErrorCodes.DuplicateSaleItem, "The product is already part of the sale.");

        var item = SaleItem.Create(productId, quantity, unitPrice);
        item.AttachTo(Id);
        _items.Add(item);

        CalculateTotal();
    }

    public decimal CalculateTotal()
    {
        Total = _items.Sum(i => i.Subtotal);
        return Total;
    }

    public void Complete()
    {
        EnsureNotCompleted();

        if (_items.Count == 0)
            throw new DomainException(DomainErrorCodes.SaleEmpty, "A sale must contain at least one item.");

        CalculateTotal();
        Status = SaleStatus.Completed;
    }

    private void EnsureNotCompleted()
    {
        if (Status == SaleStatus.Completed)
            throw new DomainException(DomainErrorCodes.SaleAlreadyCompleted, "The sale is already completed.");
    }
}
