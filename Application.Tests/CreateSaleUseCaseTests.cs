using Application.Common;
using Application.Sales;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Market.Tests.Application.Fakes;

namespace Market.Tests.Application;

[TestClass]
public class CreateSaleUseCaseTests
{
    private static readonly DateTime Now = new(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryProductRepository _products = new();
    private readonly InMemoryWarehouseStockRepository _stocks = new();
    private readonly InMemorySaleRepository _sales = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly FakeTimeProvider _time = new(Now);

    private readonly Product _milk = Product.Create("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m);
    private readonly Product _cola = Product.Create("Gaseosa Cola", "Postobón", "Bebidas", UnitType.Volume, 1.5m, 3000m, 3000m);
    private readonly WarehouseStock _l001;
    private readonly WarehouseStock _l002;
    private readonly WarehouseStock _l003;

    public CreateSaleUseCaseTests()
    {
        _products.Items.AddRange(new[] { _milk, _cola });
        _l001 = Lot(_milk, "L001", 10m, 12);
        _l002 = Lot(_milk, "L002", 20m, 42);
        _l003 = Lot(_milk, "L003", 30m, 72);
        _stocks.Items.AddRange(new[] { _l003, _l001, _l002 });
    }

    private static WarehouseStock Lot(Product product, string batch, decimal quantity, int expiresInDays) =>
        WarehouseStock.Create(product.Id, batch, "A-01", quantity, 1m, Now.AddDays(-20), Now.AddDays(expiresInDays));

    private CreateSaleUseCase UseCase() => new(_products, _stocks, _sales, _uow, _currentUser, _time);

    private static CreateSaleRequest Request(params (Guid ProductId, decimal Quantity)[] items) =>
        new(items.Select(i => new CreateSaleItemRequest(i.ProductId, i.Quantity)).ToList());

    [TestMethod]
    public async Task ShouldConsumeStockUsingFefo()
    {
        var response = await UseCase().ExecuteAsync(Request((_milk.Id, 15m)));

        Assert.AreEqual(0m, _l001.Quantity);
        Assert.AreEqual(15m, _l002.Quantity);
        Assert.AreEqual(30m, _l003.Quantity);
        Assert.AreEqual(48000m, response.Total);
    }

    [TestMethod]
    public async Task ShouldTakeUserAndPriceFromTheBackend()
    {
        var response = await UseCase().ExecuteAsync(Request((_milk.Id, 2m)));

        var sale = _sales.Items.Single();
        Assert.AreEqual(_currentUser.UserId, sale.UserId);
        Assert.AreEqual(_currentUser.UserId, response.UserId);
        Assert.AreEqual(3200m, sale.Items.Single().UnitPrice);
        Assert.AreEqual("Completed", response.Status);
        Assert.AreEqual(Now, sale.SaleDate);
    }

    [TestMethod]
    public async Task ShouldKeepHistoricalPriceWhenTheProductPriceChangesLater()
    {
        await UseCase().ExecuteAsync(Request((_milk.Id, 2m)));

        _milk.ChangePrice(9999m);

        Assert.AreEqual(3200m, _sales.Items.Single().Items.Single().UnitPrice);
    }

    [TestMethod]
    public async Task ShouldSellSeveralProductsInOneSale()
    {
        _stocks.Items.Add(Lot(_cola, "G001", 60m, 150));

        var response = await UseCase().ExecuteAsync(Request((_milk.Id, 4m), (_cola.Id, 1m)));

        Assert.HasCount(2, response.Items);
        Assert.AreEqual(15800m, response.Total);
        Assert.AreEqual("Leche Entera", response.Items.First(i => i.ProductId == _milk.Id).ProductName);
    }

    [TestMethod]
    public async Task ShouldMergeDuplicatedProductsInTheRequest()
    {
        var response = await UseCase().ExecuteAsync(Request((_milk.Id, 5m), (_milk.Id, 10m)));

        Assert.HasCount(1, response.Items);
        Assert.AreEqual(15m, response.Items.Single().Quantity);
        Assert.AreEqual(0m, _l001.Quantity);
    }

    [TestMethod]
    public async Task ShouldRunInsideATransactionAndSaveOnce()
    {
        await UseCase().ExecuteAsync(Request((_milk.Id, 2m)));

        Assert.AreEqual(1, _uow.TransactionCount);
        Assert.AreEqual(1, _uow.SaveCount);
    }

    [TestMethod]
    public async Task ShouldRejectEmptySale()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => UseCase().ExecuteAsync(Request()));

        Assert.AreEqual(DomainErrorCodes.SaleEmpty, ex.Code);
        Assert.IsEmpty(_sales.Items);
    }

    [TestMethod]
    public async Task ShouldRejectNonPositiveQuantity()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => UseCase().ExecuteAsync(Request((_milk.Id, 0m))));

        Assert.AreEqual(DomainErrorCodes.InvalidQuantity, ex.Code);
    }

    [TestMethod]
    public async Task ShouldRejectFractionalQuantityForUnitProducts()
    {
        var bread = Product.Create("Pan Tajado", "Bimbo", "Panadería", UnitType.Unit, 1m, 4500m, 6200m);
        _products.Items.Add(bread);
        _stocks.Items.Add(Lot(bread, "P001", 40m, 6));

        var ex = await Assert.ThrowsAsync<DomainException>(() => UseCase().ExecuteAsync(Request((bread.Id, 1.5m))));

        Assert.AreEqual(DomainErrorCodes.InvalidQuantity, ex.Code);
    }

    [TestMethod]
    public async Task ShouldRejectUnknownProduct()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => UseCase().ExecuteAsync(Request((Guid.NewGuid(), 1m))));

        Assert.AreEqual(AppErrorCodes.ProductNotFound, ex.Code);
        Assert.AreEqual(AppErrorKind.NotFound, ex.Kind);
    }

    [TestMethod]
    public async Task ShouldRejectInactiveProduct()
    {
        _milk.Deactivate();

        var ex = await Assert.ThrowsAsync<AppException>(() => UseCase().ExecuteAsync(Request((_milk.Id, 1m))));

        Assert.AreEqual(AppErrorCodes.ProductInactive, ex.Code);
    }

    [TestMethod]
    public async Task ShouldRejectWhenStockIsInsufficientAndLeaveInventoryUntouched()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => UseCase().ExecuteAsync(Request((_milk.Id, 61m))));

        Assert.AreEqual(DomainErrorCodes.InsufficientStock, ex.Code);
        Assert.AreEqual(10m, _l001.Quantity);
        Assert.AreEqual(20m, _l002.Quantity);
        Assert.AreEqual(30m, _l003.Quantity);
        Assert.IsEmpty(_sales.Items);
        Assert.AreEqual(0, _uow.SaveCount);
    }

    [TestMethod]
    public async Task ShouldNotConsumeAnyStockWhenASecondItemFails()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            UseCase().ExecuteAsync(Request((_milk.Id, 5m), (_cola.Id, 1m))));

        Assert.AreEqual(DomainErrorCodes.InsufficientStock, ex.Code);
        Assert.AreEqual(10m, _l001.Quantity);
        Assert.IsEmpty(_sales.Items);
    }

    [TestMethod]
    public async Task ShouldRejectWhenTheOnlyStockIsExpired()
    {
        _stocks.Items.Add(Lot(_cola, "OLD", 50m, -3));

        var ex = await Assert.ThrowsAsync<DomainException>(() => UseCase().ExecuteAsync(Request((_cola.Id, 1m))));

        Assert.AreEqual(DomainErrorCodes.InventoryExpired, ex.Code);
    }

    [TestMethod]
    public async Task ShouldIgnoreExpiredBatchesWhenValidOnesExist()
    {
        var expired = Lot(_milk, "OLD", 100m, -3);
        _stocks.Items.Add(expired);

        await UseCase().ExecuteAsync(Request((_milk.Id, 15m)));

        Assert.AreEqual(100m, expired.Quantity);
        Assert.AreEqual(0m, _l001.Quantity);
    }

    [TestMethod]
    public async Task ShouldRetryWhenAConcurrentSaleWonTheRaceButStockIsStillEnough()
    {
        _uow.ConflictsToRaise = 1;
        _uow.OnConflict = () =>
        {
            _sales.Items.Clear();
            _l001.AddQuantity(10m);
            _l002.AddQuantity(5m);
        };

        var response = await UseCase().ExecuteAsync(Request((_milk.Id, 15m)));

        Assert.AreEqual(48000m, response.Total);
        Assert.AreEqual(2, _uow.TransactionCount);
        Assert.AreEqual(1, _uow.SaveCount);
        Assert.HasCount(1, _sales.Items);
    }

    [TestMethod]
    public async Task ShouldReportInsufficientStockWhenTheConcurrentSaleTookTheStock()
    {
        _stocks.Items.Remove(_l002);
        _stocks.Items.Remove(_l003);
        _uow.ConflictsToRaise = 1;
        _uow.OnConflict = () =>
        {
            _sales.Items.Clear();
            _l001.RemoveQuantity(10m);
        };

        var ex = await Assert.ThrowsAsync<DomainException>(() => UseCase().ExecuteAsync(Request((_milk.Id, 10m))));

        Assert.AreEqual(DomainErrorCodes.InsufficientStock, ex.Code);
        Assert.IsEmpty(_sales.Items);
    }

    [TestMethod]
    public async Task ShouldGiveUpWithConflictAfterTooManyConcurrencyFailures()
    {
        _uow.ConflictsToRaise = 10;
        _uow.OnConflict = () => { };

        var ex = await Assert.ThrowsAsync<AppException>(() => UseCase().ExecuteAsync(Request((_milk.Id, 1m))));

        Assert.AreEqual(AppErrorCodes.ConcurrencyConflict, ex.Code);
        Assert.AreEqual(AppErrorKind.Conflict, ex.Kind);
        Assert.AreEqual(CreateSaleUseCase.MaxAttempts, _uow.TransactionCount);
    }
}
