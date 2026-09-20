using Domain.Enums;
using Domain.Exceptions;

namespace Domain.Entities;

public class Product
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    public string Brand { get; private set; } = null!;

    public string Category { get; private set; } = null!;

    public UnitType UnitType { get; private set; }

    public decimal UnitValue { get; private set; }

    public decimal Cost { get; private set; }

    public decimal SalePrice { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? UpdatedAt { get; private set; }

    private Product()
    {
    }

    public static Product Create(
        string name,
        string brand,
        string category,
        UnitType unitType,
        decimal unitValue,
        decimal cost,
        decimal salePrice)
    {
        ValidateInformation(name, brand, category, unitType, unitValue);
        ValidatePricing(cost, salePrice);

        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Brand = brand.Trim(),
            Category = category.Trim(),
            UnitType = unitType,
            UnitValue = unitValue,
            Cost = cost,
            SalePrice = salePrice,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void ChangePrice(decimal newSalePrice)
    {
        ValidatePricing(Cost, newSalePrice);

        SalePrice = newSalePrice;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangePricing(decimal newCost, decimal newSalePrice)
    {
        ValidatePricing(newCost, newSalePrice);

        Cost = newCost;
        SalePrice = newSalePrice;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateInformation(
        string name,
        string brand,
        string category,
        UnitType unitType,
        decimal unitValue)
    {
        ValidateInformation(name, brand, category, unitType, unitValue);

        Name = name.Trim();
        Brand = brand.Trim();
        Category = category.Trim();
        UnitType = unitType;
        UnitValue = unitValue;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(
        string name,
        string brand,
        string category,
        UnitType unitType,
        decimal unitValue,
        decimal cost,
        decimal salePrice)
    {
        ValidateInformation(name, brand, category, unitType, unitValue);
        ValidatePricing(cost, salePrice);

        Name = name.Trim();
        Brand = brand.Trim();
        Category = category.Trim();
        UnitType = unitType;
        UnitValue = unitValue;
        Cost = cost;
        SalePrice = salePrice;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool IsValidQuantity(decimal quantity)
    {
        if (quantity <= 0)
            return false;

        return UnitType != UnitType.Unit || quantity == decimal.Truncate(quantity);
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static void ValidateInformation(
        string name,
        string brand,
        string category,
        UnitType unitType,
        decimal unitValue)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(DomainErrorCodes.InvalidProduct, "Product name is required.");

        if (string.IsNullOrWhiteSpace(brand))
            throw new DomainException(DomainErrorCodes.InvalidProduct, "Product brand is required.");

        if (string.IsNullOrWhiteSpace(category))
            throw new DomainException(DomainErrorCodes.InvalidProduct, "Product category is required.");

        if (!Enum.IsDefined(unitType))
            throw new DomainException(DomainErrorCodes.InvalidProduct, "Unit type is not valid.");

        if (unitValue <= 0)
            throw new DomainException(DomainErrorCodes.InvalidProduct, "Unit value must be greater than zero.");
    }

    private static void ValidatePricing(decimal cost, decimal salePrice)
    {
        if (cost < 0)
            throw new DomainException(DomainErrorCodes.InvalidProductPrice, "Cost cannot be negative.");

        if (salePrice < cost)
            throw new DomainException(DomainErrorCodes.InvalidProductPrice, "Sale price cannot be lower than cost.");
    }
}
