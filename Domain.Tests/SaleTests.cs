using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;

namespace Market.Tests.Domain;

[TestClass]
public class SaleTests
{
    private static readonly DateTime SaleDate = new(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);

    private static Sale CreateSale() => Sale.Create(Guid.NewGuid(), SaleDate);

    [TestMethod]
    public void Create_ShouldStartIncompleteWithoutItems()
    {
        var userId = Guid.NewGuid();

        var sale = Sale.Create(userId, SaleDate);

        Assert.AreNotEqual(Guid.Empty, sale.Id);
        Assert.AreEqual(userId, sale.UserId);
        Assert.AreEqual(SaleDate, sale.SaleDate);
        Assert.AreEqual(SaleStatus.Incomplete, sale.Status);
        Assert.AreEqual(0m, sale.Total);
        Assert.IsEmpty(sale.Items);
    }

    [TestMethod]
    public void Create_ShouldRejectEmptyUserId()
    {
        Assert.Throws<DomainException>(() => Sale.Create(Guid.Empty, SaleDate));
    }

    [TestMethod]
    public void AddItem_ShouldAddItemAndUpdateTotal()
    {
        var sale = CreateSale();

        sale.AddItem(Guid.NewGuid(), 2m, 3200m);

        Assert.HasCount(1, sale.Items);
        Assert.AreEqual(6400m, sale.Total);
    }

    [TestMethod]
    public void AddItem_ShouldLinkItemToSale()
    {
        var sale = CreateSale();

        sale.AddItem(Guid.NewGuid(), 2m, 3200m);

        Assert.AreEqual(sale.Id, sale.Items.Single().SaleId);
    }

    [TestMethod]
    public void AddItem_ShouldRejectDuplicatedProduct()
    {
        var sale = CreateSale();
        var productId = Guid.NewGuid();
        sale.AddItem(productId, 1m, 3200m);

        var ex = Assert.Throws<DomainException>(() => sale.AddItem(productId, 1m, 3200m));

        Assert.AreEqual(DomainErrorCodes.DuplicateSaleItem, ex.Code);
        Assert.HasCount(1, sale.Items);
    }

    [TestMethod]
    public void AddItem_ShouldRejectInvalidQuantity()
    {
        var sale = CreateSale();

        Assert.Throws<DomainException>(() => sale.AddItem(Guid.NewGuid(), 0m, 3200m));
        Assert.IsEmpty(sale.Items);
    }

    [TestMethod]
    public void CalculateTotal_ShouldSumAllSubtotals()
    {
        var sale = CreateSale();
        sale.AddItem(Guid.NewGuid(), 4m, 3200m);
        sale.AddItem(Guid.NewGuid(), 1m, 3000m);

        var total = sale.CalculateTotal();

        Assert.AreEqual(15800m, total);
        Assert.AreEqual(15800m, sale.Total);
    }

    [TestMethod]
    public void Complete_ShouldRejectSaleWithoutItems()
    {
        var sale = CreateSale();

        var ex = Assert.Throws<DomainException>(() => sale.Complete());

        Assert.AreEqual(DomainErrorCodes.SaleEmpty, ex.Code);
        Assert.AreEqual(SaleStatus.Incomplete, sale.Status);
    }

    [TestMethod]
    public void Complete_ShouldMarkSaleAsCompletedAndCalculateTotal()
    {
        var sale = CreateSale();
        sale.AddItem(Guid.NewGuid(), 2m, 3200m);

        sale.Complete();

        Assert.AreEqual(SaleStatus.Completed, sale.Status);
        Assert.AreEqual(6400m, sale.Total);
    }

    [TestMethod]
    public void Complete_ShouldRejectAlreadyCompletedSale()
    {
        var sale = CreateSale();
        sale.AddItem(Guid.NewGuid(), 2m, 3200m);
        sale.Complete();

        var ex = Assert.Throws<DomainException>(() => sale.Complete());

        Assert.AreEqual(DomainErrorCodes.SaleAlreadyCompleted, ex.Code);
    }

    [TestMethod]
    public void AddItem_ShouldRejectCompletedSale()
    {
        var sale = CreateSale();
        sale.AddItem(Guid.NewGuid(), 2m, 3200m);
        sale.Complete();

        var ex = Assert.Throws<DomainException>(() => sale.AddItem(Guid.NewGuid(), 1m, 1000m));

        Assert.AreEqual(DomainErrorCodes.SaleAlreadyCompleted, ex.Code);
    }

    [TestMethod]
    public void Items_ShouldKeepHistoricalUnitPrice()
    {
        var sale = CreateSale();
        var productId = Guid.NewGuid();

        sale.AddItem(productId, 2m, 5900m);

        Assert.AreEqual(5900m, sale.Items.Single().UnitPrice);
    }
}
