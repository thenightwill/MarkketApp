using Domain.Entities;
using Domain.Exceptions;
using Domain.Services;

namespace Market.Tests.Domain;

[TestClass]
public class FefoAllocatorTests
{
    private static readonly DateTime Now = new(2026, 9, 19, 0, 0, 0, DateTimeKind.Utc);
    private static readonly Guid ProductId = Guid.NewGuid();

    private static WarehouseStock Lot(string batch, decimal quantity, int expiresInDays, int receivedDaysAgo = 30) =>
        WarehouseStock.Create(
            ProductId,
            batch,
            "A-01",
            quantity,
            1m,
            Now.AddDays(-receivedDaysAgo),
            Now.AddDays(expiresInDays));

    [TestMethod]
    public void Allocate_ShouldConsumeEarliestExpirationFirst()
    {
        var l001 = Lot("L001", 10m, 12);
        var l002 = Lot("L002", 20m, 42);
        var l003 = Lot("L003", 30m, 72);

        var allocations = FefoAllocator.Allocate(new[] { l001, l002, l003 }, 15m, Now);

        Assert.HasCount(2, allocations);
        Assert.AreSame(l001, allocations[0].Stock);
        Assert.AreEqual(10m, allocations[0].Quantity);
        Assert.AreSame(l002, allocations[1].Stock);
        Assert.AreEqual(5m, allocations[1].Quantity);
    }

    [TestMethod]
    public void Allocate_ShouldNotDependOnInputOrder()
    {
        var l001 = Lot("L001", 10m, 12);
        var l002 = Lot("L002", 20m, 42);
        var l003 = Lot("L003", 30m, 72);

        var allocations = FefoAllocator.Allocate(new[] { l003, l002, l001 }, 15m, Now);

        Assert.AreSame(l001, allocations[0].Stock);
        Assert.AreSame(l002, allocations[1].Stock);
    }

    [TestMethod]
    public void Allocate_ShouldUseSingleLotWhenItCoversTheRequest()
    {
        var l001 = Lot("L001", 10m, 12);
        var l002 = Lot("L002", 20m, 42);

        var allocations = FefoAllocator.Allocate(new[] { l001, l002 }, 4m, Now);

        Assert.HasCount(1, allocations);
        Assert.AreEqual(4m, allocations[0].Quantity);
    }

    [TestMethod]
    public void Allocate_ShouldNotMutateStock()
    {
        var l001 = Lot("L001", 10m, 12);

        FefoAllocator.Allocate(new[] { l001 }, 4m, Now);

        Assert.AreEqual(10m, l001.Quantity);
    }

    [TestMethod]
    public void Allocate_ShouldSkipExpiredLots()
    {
        var expired = Lot("OLD", 50m, -2);
        var valid = Lot("L001", 10m, 12);

        var allocations = FefoAllocator.Allocate(new[] { expired, valid }, 5m, Now);

        Assert.HasCount(1, allocations);
        Assert.AreSame(valid, allocations[0].Stock);
    }

    [TestMethod]
    public void Allocate_ShouldSkipInactiveAndEmptyLots()
    {
        var inactive = Lot("INA", 50m, 5);
        inactive.Deactivate();
        var empty = Lot("EMP", 5m, 6);
        empty.RemoveQuantity(5m);
        var valid = Lot("L001", 10m, 12);

        var allocations = FefoAllocator.Allocate(new[] { inactive, empty, valid }, 5m, Now);

        Assert.HasCount(1, allocations);
        Assert.AreSame(valid, allocations[0].Stock);
    }

    [TestMethod]
    public void Allocate_ShouldBreakTiesUsingReceivedDate()
    {
        var newer = Lot("NEW", 10m, 12, receivedDaysAgo: 2);
        var older = Lot("OLD", 10m, 12, receivedDaysAgo: 20);

        var allocations = FefoAllocator.Allocate(new[] { newer, older }, 5m, Now);

        Assert.AreSame(older, allocations[0].Stock);
    }

    [TestMethod]
    public void Allocate_ShouldRejectWhenStockIsInsufficient()
    {
        var l001 = Lot("L001", 10m, 12);

        var ex = Assert.Throws<DomainException>(() => FefoAllocator.Allocate(new[] { l001 }, 11m, Now));

        Assert.AreEqual(DomainErrorCodes.InsufficientStock, ex.Code);
    }

    [TestMethod]
    public void Allocate_ShouldRejectWhenThereAreNoLots()
    {
        var ex = Assert.Throws<DomainException>(() => FefoAllocator.Allocate(Array.Empty<WarehouseStock>(), 1m, Now));

        Assert.AreEqual(DomainErrorCodes.InsufficientStock, ex.Code);
    }

    [TestMethod]
    public void Allocate_ShouldRejectWhenOnlyExpiredStockExists()
    {
        var expired = Lot("OLD", 50m, -2);

        var ex = Assert.Throws<DomainException>(() => FefoAllocator.Allocate(new[] { expired }, 1m, Now));

        Assert.AreEqual(DomainErrorCodes.InventoryExpired, ex.Code);
    }

    [TestMethod]
    public void Allocate_ShouldReportInsufficientStockWhenValidStockExistsButIsNotEnough()
    {
        var expired = Lot("OLD", 50m, -2);
        var valid = Lot("L001", 3m, 12);

        var ex = Assert.Throws<DomainException>(() => FefoAllocator.Allocate(new[] { expired, valid }, 5m, Now));

        Assert.AreEqual(DomainErrorCodes.InsufficientStock, ex.Code);
    }

    [TestMethod]
    public void Allocate_ShouldRejectNonPositiveQuantity()
    {
        var l001 = Lot("L001", 10m, 12);

        var ex = Assert.Throws<DomainException>(() => FefoAllocator.Allocate(new[] { l001 }, 0m, Now));

        Assert.AreEqual(DomainErrorCodes.InvalidQuantity, ex.Code);
    }

    [TestMethod]
    public void Allocate_ShouldAllowConsumingAllAvailableStock()
    {
        var l001 = Lot("L001", 10m, 12);
        var l002 = Lot("L002", 20m, 42);

        var allocations = FefoAllocator.Allocate(new[] { l001, l002 }, 30m, Now);

        Assert.AreEqual(30m, allocations.Sum(a => a.Quantity));
    }
}
