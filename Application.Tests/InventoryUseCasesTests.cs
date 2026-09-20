using Application.Common;
using Application.Inventory;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Market.Tests.Application.Fakes;

namespace Market.Tests.Application;

[TestClass]
public class InventoryUseCasesTests
{
    private static readonly DateTime Now = new(2026, 9, 19, 12, 0, 0, DateTimeKind.Utc);

    private readonly InMemoryProductRepository _products = new();
    private readonly InMemoryWarehouseStockRepository _stocks = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeTimeProvider _time = new(Now);
    private readonly Product _milk = Product.Create("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m);

    public InventoryUseCasesTests() => _products.Items.Add(_milk);

    private CreateStockUseCase Create() => new(_products, _stocks, _uow, _time);

    private UpdateStockUseCase Update() => new(_stocks, _products, _uow, _time);

    private AddStockQuantityUseCase AddQuantity() => new(_stocks, _products, _uow, _time);

    private CreateStockRequest Request(Guid? productId = null, string batch = "L001", decimal quantity = 10m) =>
        new(productId ?? _milk.Id, batch, "A-01", quantity, 5m, Now.AddDays(-5), Now.AddDays(30));

    [TestMethod]
    public async Task Create_ShouldRegisterBatchForActiveProduct()
    {
        var response = await Create().ExecuteAsync(Request());

        var stock = _stocks.Items.Single();
        Assert.AreEqual(stock.Id, response.Id);
        Assert.AreEqual(_milk.Id, response.ProductId);
        Assert.AreEqual("Leche Entera", response.ProductName);
        Assert.AreEqual(10m, response.Quantity);
        Assert.IsFalse(response.IsExpired);
        Assert.IsFalse(response.IsLowStock);
        Assert.AreEqual(1, _uow.SaveCount);
    }

    [TestMethod]
    public async Task Create_ShouldRejectUnknownProduct()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Create().ExecuteAsync(Request(productId: Guid.NewGuid())));

        Assert.AreEqual(AppErrorCodes.ProductNotFound, ex.Code);
    }

    [TestMethod]
    public async Task Create_ShouldRejectInactiveProduct()
    {
        _milk.Deactivate();

        var ex = await Assert.ThrowsAsync<AppException>(() => Create().ExecuteAsync(Request()));

        Assert.AreEqual(AppErrorCodes.ProductInactive, ex.Code);
        Assert.AreEqual(AppErrorKind.Conflict, ex.Kind);
    }

    [TestMethod]
    public async Task Create_ShouldRejectDuplicatedBatchNumberForTheSameProduct()
    {
        await Create().ExecuteAsync(Request());

        var ex = await Assert.ThrowsAsync<AppException>(() => Create().ExecuteAsync(Request()));

        Assert.AreEqual(AppErrorCodes.DuplicateBatch, ex.Code);
    }

    [TestMethod]
    public async Task Create_ShouldAllowSameBatchNumberForDifferentProducts()
    {
        var yogurt = Product.Create("Yogurt", "Alpina", "Lácteos", UnitType.Volume, 1m, 1000m, 1500m);
        _products.Items.Add(yogurt);
        await Create().ExecuteAsync(Request());

        await Create().ExecuteAsync(Request(productId: yogurt.Id));

        Assert.HasCount(2, _stocks.Items);
    }

    [TestMethod]
    public async Task Create_ShouldRejectNonPositiveQuantity()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => Create().ExecuteAsync(Request(quantity: 0m)));

        Assert.AreEqual(DomainErrorCodes.InvalidQuantity, ex.Code);
    }

    [TestMethod]
    public async Task Create_ShouldRejectFractionalQuantityForUnitProducts()
    {
        var bread = Product.Create("Pan Tajado", "Bimbo", "Panadería", UnitType.Unit, 1m, 4500m, 6200m);
        _products.Items.Add(bread);

        var ex = await Assert.ThrowsAsync<DomainException>(() => Create().ExecuteAsync(Request(productId: bread.Id, quantity: 2.5m)));

        Assert.AreEqual(DomainErrorCodes.InvalidQuantity, ex.Code);
    }

    [TestMethod]
    public async Task Update_ShouldChangeDetailsButNeverTheQuantity()
    {
        var created = await Create().ExecuteAsync(Request());

        var response = await Update().ExecuteAsync(
            created.Id,
            new UpdateStockRequest("B-02", 8m, Now.AddDays(60), true));

        Assert.AreEqual("B-02", response.Location);
        Assert.AreEqual(8m, response.MinimumStock);
        Assert.AreEqual(10m, response.Quantity);
    }

    [TestMethod]
    public async Task Update_ShouldDeactivateBatch()
    {
        var created = await Create().ExecuteAsync(Request());

        var response = await Update().ExecuteAsync(
            created.Id,
            new UpdateStockRequest("A-01", 5m, Now.AddDays(30), false));

        Assert.IsFalse(response.IsActive);
    }

    [TestMethod]
    public async Task Update_ShouldReturnNotFoundForUnknownBatch()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Update().ExecuteAsync(
            Guid.NewGuid(),
            new UpdateStockRequest("A-01", 5m, Now.AddDays(30), true)));

        Assert.AreEqual(AppErrorCodes.InventoryNotFound, ex.Code);
        Assert.AreEqual(AppErrorKind.NotFound, ex.Kind);
    }

    [TestMethod]
    public async Task Update_ShouldRejectInvalidExpirationDate()
    {
        var created = await Create().ExecuteAsync(Request());

        await Assert.ThrowsAsync<DomainException>(() => Update().ExecuteAsync(
            created.Id,
            new UpdateStockRequest("A-01", 5m, Now.AddDays(-30), true)));

        Assert.AreEqual(Now.AddDays(30), _stocks.Items.Single().ExpirationDate);
    }

    [TestMethod]
    public async Task GetStocks_ShouldFlagLowStockAndExpiredBatches()
    {
        _stocks.Items.Add(WarehouseStock.Create(_milk.Id, "LOW", "A-01", 3m, 5m, Now.AddDays(-10), Now.AddDays(20)));
        _stocks.Items.Add(WarehouseStock.Create(_milk.Id, "OLD", "A-01", 8m, 2m, Now.AddDays(-40), Now.AddDays(-5)));
        _stocks.Items.Add(WarehouseStock.Create(_milk.Id, "OK", "A-01", 50m, 2m, Now.AddDays(-2), Now.AddDays(40)));
        var query = new GetStocksUseCase(_stocks, _products, _time);

        var all = await query.ExecuteAsync(new StockFilter(null, null, null));
        var low = await query.ExecuteAsync(new StockFilter(null, true, null));
        var expired = await query.ExecuteAsync(new StockFilter(null, null, true));

        Assert.HasCount(3, all);
        Assert.AreEqual("LOW", low.Single().BatchNumber);
        Assert.AreEqual("OLD", expired.Single().BatchNumber);
        Assert.IsTrue(all.Single(s => s.BatchNumber == "OLD").IsExpired);
    }

    [TestMethod]
    public async Task GetStocks_ShouldFilterByProduct()
    {
        var yogurt = Product.Create("Yogurt", "Alpina", "Lácteos", UnitType.Volume, 1m, 1000m, 1500m);
        _products.Items.Add(yogurt);
        _stocks.Items.Add(WarehouseStock.Create(_milk.Id, "M1", "A-01", 10m, 2m, Now.AddDays(-1), Now.AddDays(20)));
        _stocks.Items.Add(WarehouseStock.Create(yogurt.Id, "Y1", "A-02", 10m, 2m, Now.AddDays(-1), Now.AddDays(20)));

        var result = await new GetStocksUseCase(_stocks, _products, _time).ExecuteAsync(new StockFilter(yogurt.Id, null, null));

        Assert.AreEqual("Y1", result.Single().BatchNumber);
    }

    [TestMethod]
    public async Task GetStockById_ShouldReturnBatchOrNotFound()
    {
        var created = await Create().ExecuteAsync(Request());
        var query = new GetStockByIdUseCase(_stocks, _products, _time);

        var response = await query.ExecuteAsync(created.Id);
        var ex = await Assert.ThrowsAsync<AppException>(() => query.ExecuteAsync(Guid.NewGuid()));

        Assert.AreEqual("L001", response.BatchNumber);
        Assert.AreEqual(AppErrorCodes.InventoryNotFound, ex.Code);
    }

    [TestMethod]
    public async Task AddQuantity_ShouldIncreaseTheBatchQuantity()
    {
        var created = await Create().ExecuteAsync(Request(quantity: 10m));

        var response = await AddQuantity().ExecuteAsync(created.Id, new AddStockQuantityRequest(5m));

        Assert.AreEqual(15m, response.Quantity);
        Assert.AreEqual(15m, _stocks.Items.Single().Quantity);
    }

    [TestMethod]
    public async Task AddQuantity_ShouldReturnNotFoundForUnknownBatch()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            AddQuantity().ExecuteAsync(Guid.NewGuid(), new AddStockQuantityRequest(5m)));

        Assert.AreEqual(AppErrorCodes.InventoryNotFound, ex.Code);
        Assert.AreEqual(AppErrorKind.NotFound, ex.Kind);
    }

    [TestMethod]
    public async Task AddQuantity_ShouldRejectNonPositiveQuantity()
    {
        var created = await Create().ExecuteAsync(Request(quantity: 10m));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            AddQuantity().ExecuteAsync(created.Id, new AddStockQuantityRequest(0m)));

        Assert.AreEqual(DomainErrorCodes.InvalidQuantity, ex.Code);
        Assert.AreEqual(10m, _stocks.Items.Single().Quantity);
    }

    [TestMethod]
    public async Task AddQuantity_ShouldRejectFractionalQuantityForUnitProducts()
    {
        var bread = Product.Create("Pan Tajado", "Bimbo", "Panadería", UnitType.Unit, 1m, 4500m, 6200m);
        _products.Items.Add(bread);
        var created = await Create().ExecuteAsync(Request(productId: bread.Id, batch: "P001", quantity: 10m));

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            AddQuantity().ExecuteAsync(created.Id, new AddStockQuantityRequest(1.5m)));

        Assert.AreEqual(DomainErrorCodes.InvalidQuantity, ex.Code);
    }

    [TestMethod]
    public async Task AddQuantity_ShouldWorkEvenWhenTheBatchIsInactive()
    {
        var created = await Create().ExecuteAsync(Request(quantity: 10m));
        await Update().ExecuteAsync(created.Id, new UpdateStockRequest("A-01", 5m, Now.AddDays(30), false));

        var response = await AddQuantity().ExecuteAsync(created.Id, new AddStockQuantityRequest(3m));

        Assert.AreEqual(13m, response.Quantity);
        Assert.IsFalse(response.IsActive);
    }
}
