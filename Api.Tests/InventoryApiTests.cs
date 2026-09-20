using System.Net;
using Application.Inventory;
using Application.Products;

namespace Market.Tests.Api;

[TestClass]
public class InventoryApiTests
{
    [TestMethod]
    public async Task Post_ShouldRegisterABatchForAnActiveProduct()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        var now = DateTime.UtcNow;

        var response = await admin.Client.PostJsonAsync(
            "/api/inventory",
            new CreateStockRequest(product.Id, "L001", "A-01", 25m, 5m, now.AddDays(-2), now.AddDays(40)));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var stock = await response.ReadAsync<StockResponse>();
        Assert.AreEqual(product.Name, stock.ProductName);
        Assert.AreEqual(25m, stock.Quantity);
        Assert.IsFalse(stock.IsExpired);
        Assert.IsFalse(stock.IsLowStock);
    }

    [TestMethod]
    public async Task Post_ShouldBeForbiddenForEmployees()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.SeededEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        var now = DateTime.UtcNow;

        var response = await employee.Client.PostJsonAsync(
            "/api/inventory",
            new CreateStockRequest(product.Id, "L001", "A-01", 5m, 1m, now.AddDays(-1), now.AddDays(10)));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Post_ShouldRejectDuplicatedBatchNumbersForTheSameProduct()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);
        var now = DateTime.UtcNow;

        var response = await admin.Client.PostJsonAsync(
            "/api/inventory",
            new CreateStockRequest(product.Id, "L001", "B-01", 5m, 1m, now.AddDays(-1), now.AddDays(10)));

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("DUPLICATE_BATCH", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Post_ShouldRejectInactiveAndUnknownProducts()
    {
        var admin = await ApiHelpers.AdminAsync();
        var inactive = await ApiHelpers.CreateProductAsync(admin.Client);
        await admin.Client.DeleteAsync($"/api/products/{inactive.Id}");
        var now = DateTime.UtcNow;

        var inactiveResponse = await admin.Client.PostJsonAsync(
            "/api/inventory",
            new CreateStockRequest(inactive.Id, "L001", "A-01", 5m, 1m, now.AddDays(-1), now.AddDays(10)));
        var unknownResponse = await admin.Client.PostJsonAsync(
            "/api/inventory",
            new CreateStockRequest(Guid.NewGuid(), "L001", "A-01", 5m, 1m, now.AddDays(-1), now.AddDays(10)));

        Assert.AreEqual(HttpStatusCode.Conflict, inactiveResponse.StatusCode);
        Assert.AreEqual("PRODUCT_INACTIVE", await inactiveResponse.CodeAsync());
        Assert.AreEqual(HttpStatusCode.NotFound, unknownResponse.StatusCode);
        Assert.AreEqual("PRODUCT_NOT_FOUND", await unknownResponse.CodeAsync());
    }

    [TestMethod]
    public async Task Post_ShouldRejectInvalidQuantitiesAndDates()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        var now = DateTime.UtcNow;

        var zeroQuantity = await admin.Client.PostJsonAsync(
            "/api/inventory",
            new CreateStockRequest(product.Id, "L001", "A-01", 0m, 1m, now.AddDays(-1), now.AddDays(10)));
        var badDates = await admin.Client.PostJsonAsync(
            "/api/inventory",
            new CreateStockRequest(product.Id, "L002", "A-01", 5m, 1m, now.AddDays(5), now.AddDays(1)));
        var negativeMinimum = await admin.Client.PostJsonAsync(
            "/api/inventory",
            new CreateStockRequest(product.Id, "L003", "A-01", 5m, -1m, now.AddDays(-1), now.AddDays(10)));

        Assert.AreEqual(HttpStatusCode.BadRequest, zeroQuantity.StatusCode);
        Assert.AreEqual("INVALID_QUANTITY", await zeroQuantity.CodeAsync());
        Assert.AreEqual(HttpStatusCode.BadRequest, badDates.StatusCode);
        Assert.AreEqual(HttpStatusCode.BadRequest, negativeMinimum.StatusCode);
    }

    [TestMethod]
    public async Task Put_ShouldUpdateDetailsButNeverTheQuantity()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        var stock = await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);

        var response = await admin.Client.PutJsonAsync(
            $"/api/inventory/{stock.Id}",
            new { location = "Z-99", minimumStock = 4, expirationDate = DateTime.UtcNow.AddDays(90), isActive = true, quantity = 9999 });

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadAsync<StockResponse>();
        Assert.AreEqual("Z-99", updated.Location);
        Assert.AreEqual(4m, updated.MinimumStock);
        Assert.AreEqual(10m, updated.Quantity);
    }

    [TestMethod]
    public async Task Put_ShouldReturnNotFoundForUnknownBatches()
    {
        var admin = await ApiHelpers.AdminAsync();

        var response = await admin.Client.PutJsonAsync(
            $"/api/inventory/{Guid.NewGuid()}",
            new UpdateStockRequest("A-01", 1m, DateTime.UtcNow.AddDays(10), true));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual("INVENTORY_NOT_FOUND", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Get_ShouldFlagExpiredAndLowStockBatchesFromTheSeed()
    {
        var employee = await ApiHelpers.SeededEmployeeAsync();

        var all = await (await employee.Client.GetAsync("/api/inventory")).ReadAsync<List<StockResponse>>();
        var low = await (await employee.Client.GetAsync("/api/inventory?lowStock=true")).ReadAsync<List<StockResponse>>();
        var expired = await (await employee.Client.GetAsync("/api/inventory?expired=true")).ReadAsync<List<StockResponse>>();

        Assert.IsTrue(all.Any(s => s.BatchNumber == "YOG-001" && s.IsExpired));
        Assert.IsTrue(low.Any(s => s.BatchNumber == "ARR-001"));
        Assert.IsTrue(low.All(s => s.IsLowStock));
        Assert.IsTrue(expired.Any(s => s.BatchNumber == "YOG-001"));
        Assert.IsTrue(expired.All(s => s.IsExpired));
    }

    [TestMethod]
    public async Task Get_ShouldFilterByProductAndReturnById()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        var stock = await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);

        var filtered = await (await admin.Client.GetAsync($"/api/inventory?productId={product.Id}")).ReadAsync<List<StockResponse>>();
        var byId = await admin.Client.GetAsync($"/api/inventory/{stock.Id}");
        var missing = await admin.Client.GetAsync($"/api/inventory/{Guid.NewGuid()}");

        Assert.AreEqual(stock.Id, filtered.Single().Id);
        Assert.AreEqual(HttpStatusCode.OK, byId.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.AreEqual("INVENTORY_NOT_FOUND", await missing.CodeAsync());
    }

    [TestMethod]
    public async Task ThereIsNoWayToDeleteABatch()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        var stock = await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);

        var response = await admin.Client.DeleteAsync($"/api/inventory/{stock.Id}");

        Assert.AreEqual(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    }

    [TestMethod]
    public async Task PostAddQuantity_ShouldIncreaseTheBatchQuantity()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        var stock = await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);

        var response = await admin.Client.PostJsonAsync($"/api/inventory/{stock.Id}/add-quantity", new AddStockQuantityRequest(5m));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadAsync<StockResponse>();
        Assert.AreEqual(15m, updated.Quantity);
    }

    [TestMethod]
    public async Task PostAddQuantity_ShouldBeForbiddenForEmployees()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.SeededEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        var stock = await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);

        var response = await employee.Client.PostJsonAsync($"/api/inventory/{stock.Id}/add-quantity", new AddStockQuantityRequest(5m));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task PostAddQuantity_ShouldRejectNonPositiveQuantity()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        var stock = await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);

        var response = await admin.Client.PostJsonAsync($"/api/inventory/{stock.Id}/add-quantity", new AddStockQuantityRequest(0m));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual("INVALID_QUANTITY", await response.CodeAsync());
    }

    [TestMethod]
    public async Task PostAddQuantity_ShouldReturnNotFoundForUnknownBatch()
    {
        var admin = await ApiHelpers.AdminAsync();

        var response = await admin.Client.PostJsonAsync($"/api/inventory/{Guid.NewGuid()}/add-quantity", new AddStockQuantityRequest(5m));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual("INVENTORY_NOT_FOUND", await response.CodeAsync());
    }
}
