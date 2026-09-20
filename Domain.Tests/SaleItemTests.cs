using Domain.Entities;
using Domain.Exceptions;

namespace Market.Tests.Domain;

[TestClass]
public class SaleItemTests
{
    [TestMethod]
    public void Create_ShouldCalculateSubtotal()
    {
        var productId = Guid.NewGuid();

        var item = SaleItem.Create(productId, 3m, 3200m);

        Assert.AreNotEqual(Guid.Empty, item.Id);
        Assert.AreEqual(productId, item.ProductId);
        Assert.AreEqual(3m, item.Quantity);
        Assert.AreEqual(3200m, item.UnitPrice);
        Assert.AreEqual(9600m, item.Subtotal);
    }

    [TestMethod]
    public void Create_ShouldSupportFractionalQuantities()
    {
        var item = SaleItem.Create(Guid.NewGuid(), 1.5m, 4000m);

        Assert.AreEqual(6000m, item.Subtotal);
    }

    [TestMethod]
    public void Create_ShouldRejectZeroQuantity()
    {
        var ex = Assert.Throws<DomainException>(() => SaleItem.Create(Guid.NewGuid(), 0m, 3200m));

        Assert.AreEqual(DomainErrorCodes.InvalidQuantity, ex.Code);
    }

    [TestMethod]
    public void Create_ShouldRejectNegativeQuantity()
    {
        Assert.Throws<DomainException>(() => SaleItem.Create(Guid.NewGuid(), -1m, 3200m));
    }

    [TestMethod]
    public void Create_ShouldRejectNegativeUnitPrice()
    {
        Assert.Throws<DomainException>(() => SaleItem.Create(Guid.NewGuid(), 1m, -1m));
    }

    [TestMethod]
    public void Create_ShouldAcceptZeroUnitPrice()
    {
        var item = SaleItem.Create(Guid.NewGuid(), 2m, 0m);

        Assert.AreEqual(0m, item.Subtotal);
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyProductId()
    {
        Assert.Throws<DomainException>(() => SaleItem.Create(Guid.Empty, 1m, 3200m));
    }
}
