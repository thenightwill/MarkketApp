using Domain.Exceptions;

namespace Domain.Entities;

public class SaleItem
{
    public Guid Id { get; private set; }

    public Guid SaleId { get; private set; }

    public Guid ProductId { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal UnitPrice { get; private set; }

    public decimal Subtotal { get; private set; }

    private SaleItem() { }

    public static SaleItem Create(Guid productId, decimal quantity, decimal unitPrice)
    {
        if (productId == Guid.Empty)
            throw new DomainException(DomainErrorCodes.InvalidSale, "Product is required.");

        if (quantity <= 0)
            throw new DomainException(DomainErrorCodes.InvalidQuantity, "Quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new DomainException(DomainErrorCodes.InvalidSale, "Unit price cannot be negative.");

        return new SaleItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            Quantity = quantity,
            UnitPrice = unitPrice,
            Subtotal = quantity * unitPrice
        };
    }

    internal void AttachTo(Guid saleId) => SaleId = saleId;
}
