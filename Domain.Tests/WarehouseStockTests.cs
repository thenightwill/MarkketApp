using Domain.Entities;
using Domain.Exceptions;

namespace Market.Tests.Domain;

[TestClass]
public class WarehouseStockTests
{
    private static readonly DateTime Received = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Expiration = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private static WarehouseStock CreateValidStock(
        decimal quantity = 10m,
        decimal minimumStock = 5m,
        DateTime? received = null,
        DateTime? expiration = null) =>
        WarehouseStock.Create(
            Guid.NewGuid(),
            "L001",
            "A-01",
            quantity,
            minimumStock,
            received ?? Received,
            expiration ?? Expiration);

    [TestMethod]
    public void Create_ShouldCreateStockWithValidData()
    {
        var productId = Guid.NewGuid();

        var stock = WarehouseStock.Create(productId, "L001", "A-01", 10m, 5m, Received, Expiration);

        Assert.AreNotEqual(Guid.Empty, stock.Id);
        Assert.AreEqual(productId, stock.ProductId);
        Assert.AreEqual("L001", stock.BatchNumber);
        Assert.AreEqual("A-01", stock.Location);
        Assert.AreEqual(10m, stock.Quantity);
        Assert.AreEqual(5m, stock.MinimumStock);
        Assert.AreEqual(Received, stock.ReceivedDate);
        Assert.AreEqual(Expiration, stock.ExpirationDate);
        Assert.IsTrue(stock.IsActive);
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyProductId()
    {
        Assert.Throws<DomainException>(() =>
            WarehouseStock.Create(Guid.Empty, "L001", "A-01", 10m, 5m, Received, Expiration));
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyBatchNumber()
    {
        Assert.Throws<DomainException>(() =>
            WarehouseStock.Create(Guid.NewGuid(), " ", "A-01", 10m, 5m, Received, Expiration));
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyLocation()
    {
        Assert.Throws<DomainException>(() =>
            WarehouseStock.Create(Guid.NewGuid(), "L001", string.Empty, 10m, 5m, Received, Expiration));
    }

    [TestMethod]
    public void Create_ShouldRejectZeroQuantity()
    {
        var ex = Assert.Throws<DomainException>(() => CreateValidStock(quantity: 0m));

        Assert.AreEqual(DomainErrorCodes.InvalidQuantity, ex.Code);
    }

    [TestMethod]
    public void Create_ShouldRejectNegativeQuantity()
    {
        Assert.Throws<DomainException>(() => CreateValidStock(quantity: -1m));
    }

    [TestMethod]
    public void Create_ShouldRejectNegativeMinimumStock()
    {
        Assert.Throws<DomainException>(() => CreateValidStock(minimumStock: -1m));
    }

    [TestMethod]
    public void Create_ShouldAcceptZeroMinimumStock()
    {
        var stock = CreateValidStock(minimumStock: 0m);

        Assert.AreEqual(0m, stock.MinimumStock);
    }

    [TestMethod]
    public void Create_ShouldRejectExpirationEqualToReceivedDate()
    {
        Assert.Throws<DomainException>(() => CreateValidStock(expiration: Received));
    }

    [TestMethod]
    public void Create_ShouldRejectExpirationBeforeReceivedDate()
    {
        Assert.Throws<DomainException>(() => CreateValidStock(expiration: Received.AddDays(-1)));
    }

    [TestMethod]
    public void IsExpired_ShouldBeTrueAfterExpirationDate()
    {
        var stock = CreateValidStock();

        Assert.IsTrue(stock.IsExpired(Expiration.AddDays(1)));
    }

    [TestMethod]
    public void IsExpired_ShouldBeTrueAtExpirationDate()
    {
        var stock = CreateValidStock();

        Assert.IsTrue(stock.IsExpired(Expiration));
    }

    [TestMethod]
    public void IsExpired_ShouldBeFalseBeforeExpirationDate()
    {
        var stock = CreateValidStock();

        Assert.IsFalse(stock.IsExpired(Expiration.AddDays(-1)));
    }

    [TestMethod]
    public void IsLowStock_ShouldBeTrueWhenQuantityIsBelowMinimum()
    {
        var stock = CreateValidStock(quantity: 3m, minimumStock: 5m);

        Assert.IsTrue(stock.IsLowStock());
    }

    [TestMethod]
    public void IsLowStock_ShouldBeTrueWhenQuantityEqualsMinimum()
    {
        var stock = CreateValidStock(quantity: 5m, minimumStock: 5m);

        Assert.IsTrue(stock.IsLowStock());
    }

    [TestMethod]
    public void IsLowStock_ShouldBeFalseWhenQuantityIsAboveMinimum()
    {
        var stock = CreateValidStock(quantity: 6m, minimumStock: 5m);

        Assert.IsFalse(stock.IsLowStock());
    }

    [TestMethod]
    public void AddQuantity_ShouldIncreaseQuantity()
    {
        var stock = CreateValidStock(quantity: 10m);

        stock.AddQuantity(5m);

        Assert.AreEqual(15m, stock.Quantity);
    }

    [TestMethod]
    public void AddQuantity_ShouldRejectNonPositiveQuantity()
    {
        var stock = CreateValidStock(quantity: 10m);

        Assert.Throws<DomainException>(() => stock.AddQuantity(0m));
        Assert.Throws<DomainException>(() => stock.AddQuantity(-2m));
        Assert.AreEqual(10m, stock.Quantity);
    }

    [TestMethod]
    public void RemoveQuantity_ShouldDecreaseQuantity()
    {
        var stock = CreateValidStock(quantity: 10m);

        stock.RemoveQuantity(4m);

        Assert.AreEqual(6m, stock.Quantity);
    }

    [TestMethod]
    public void RemoveQuantity_ShouldAllowRemovingAllQuantity()
    {
        var stock = CreateValidStock(quantity: 10m);

        stock.RemoveQuantity(10m);

        Assert.AreEqual(0m, stock.Quantity);
    }

    [TestMethod]
    public void RemoveQuantity_ShouldRejectMoreThanAvailable()
    {
        var stock = CreateValidStock(quantity: 10m);

        var ex = Assert.Throws<DomainException>(() => stock.RemoveQuantity(11m));

        Assert.AreEqual(DomainErrorCodes.InsufficientStock, ex.Code);
        Assert.AreEqual(10m, stock.Quantity);
    }

    [TestMethod]
    public void RemoveQuantity_ShouldRejectNonPositiveQuantity()
    {
        var stock = CreateValidStock(quantity: 10m);

        var ex = Assert.Throws<DomainException>(() => stock.RemoveQuantity(0m));

        Assert.AreEqual(DomainErrorCodes.InvalidQuantity, ex.Code);
    }

    [TestMethod]
    public void UpdateDetails_ShouldUpdateLocationMinimumStockAndExpiration()
    {
        var stock = CreateValidStock();
        var newExpiration = Expiration.AddDays(10);

        stock.UpdateDetails("B-02", 8m, newExpiration);

        Assert.AreEqual("B-02", stock.Location);
        Assert.AreEqual(8m, stock.MinimumStock);
        Assert.AreEqual(newExpiration, stock.ExpirationDate);
        Assert.AreEqual(10m, stock.Quantity);
    }

    [TestMethod]
    public void UpdateDetails_ShouldNotModifyStockWhenDataIsInvalid()
    {
        var stock = CreateValidStock();

        Assert.Throws<DomainException>(() => stock.UpdateDetails("B-02", 8m, Received.AddDays(-1)));

        Assert.AreEqual("A-01", stock.Location);
        Assert.AreEqual(5m, stock.MinimumStock);
        Assert.AreEqual(Expiration, stock.ExpirationDate);
    }

    [TestMethod]
    public void UpdateDetails_ShouldRejectNegativeMinimumStock()
    {
        var stock = CreateValidStock();

        Assert.Throws<DomainException>(() => stock.UpdateDetails("B-02", -1m, Expiration));
    }

    [TestMethod]
    public void UpdateDetails_ShouldRejectEmptyLocation()
    {
        var stock = CreateValidStock();

        Assert.Throws<DomainException>(() => stock.UpdateDetails(" ", 5m, Expiration));
    }

    [TestMethod]
    public void Deactivate_And_Activate_ShouldToggleIsActive()
    {
        var stock = CreateValidStock();

        stock.Deactivate();
        Assert.IsFalse(stock.IsActive);

        stock.Activate();
        Assert.IsTrue(stock.IsActive);
    }
}
