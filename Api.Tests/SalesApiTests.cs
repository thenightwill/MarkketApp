using System.Net;
using Application.Inventory;
using Application.Sales;
using Domain.Enums;

namespace Market.Tests.Api;

[TestClass]
public class SalesApiTests
{
    private static Task<HttpResponseMessage> Sell(HttpClient client, Guid productId, decimal quantity) =>
        client.PostJsonAsync("/api/sales", new CreateSaleRequest(new[] { new CreateSaleItemRequest(productId, quantity) }));

    [TestMethod]
    public async Task Post_ShouldSellUsingFefoAndUpdateInventory()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.NewEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client, salePrice: 3200m);
        var l003 = await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L003", 30m, expiresInDays: 72);
        var l001 = await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m, expiresInDays: 12);
        var l002 = await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L002", 20m, expiresInDays: 42);

        var response = await Sell(employee.Client, product.Id, 15m);

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var sale = await response.ReadAsync<SaleResponse>();
        Assert.AreEqual(48000m, sale.Total);
        Assert.AreEqual("Completed", sale.Status);
        Assert.AreEqual(3200m, sale.Items.Single().UnitPrice);

        var stocks = await (await admin.Client.GetAsync($"/api/inventory?productId={product.Id}")).ReadAsync<List<StockResponse>>();
        Assert.AreEqual(0m, stocks.Single(s => s.Id == l001.Id).Quantity);
        Assert.AreEqual(15m, stocks.Single(s => s.Id == l002.Id).Quantity);
        Assert.AreEqual(30m, stocks.Single(s => s.Id == l003.Id).Quantity);
    }

    [TestMethod]
    public async Task Post_ShouldIgnoreUserIdAndPricesSentByTheClient()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.NewEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client, salePrice: 3200m);
        await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);

        var response = await employee.Client.PostJsonAsync(
            "/api/sales",
            new
            {
                userId = Guid.NewGuid(),
                items = new[] { new { productId = product.Id, quantity = 2, unitPrice = 1, batchNumber = "HACK" } }
            });

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var sale = await response.ReadAsync<SaleResponse>();
        Assert.AreEqual(employee.UserId, sale.UserId);
        Assert.AreEqual(3200m, sale.Items.Single().UnitPrice);
        Assert.AreEqual(6400m, sale.Total);
    }

    [TestMethod]
    public async Task Post_ShouldKeepTheHistoricalPriceAfterThePriceChanges()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.NewEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client, salePrice: 3200m);
        await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);
        var sale = await (await Sell(employee.Client, product.Id, 1m)).ReadAsync<SaleResponse>();

        await admin.Client.PutJsonAsync(
            $"/api/products/{product.Id}",
            new Application.Products.UpdateProductRequest(product.Name, product.Brand, product.Category, UnitType.Volume, 1m, 2500m, 9000m, true));
        var reloaded = await (await employee.Client.GetAsync($"/api/sales/{sale.Id}")).ReadAsync<SaleResponse>();

        Assert.AreEqual(3200m, reloaded.Items.Single().UnitPrice);
    }

    [TestMethod]
    public async Task Post_ShouldRejectSalesWithoutStock()
    {
        var employee = await ApiHelpers.SeededEmployeeAsync();
        var products = await (await employee.Client.GetAsync("/api/products")).ReadAsync<List<Application.Products.ProductResponse>>();
        var detergent = products.Single(p => p.Name == "Detergente Líquido");

        var response = await Sell(employee.Client, detergent.Id, 1m);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("INSUFFICIENT_STOCK", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Post_ShouldRejectProductsWhoseOnlyStockIsExpired()
    {
        var employee = await ApiHelpers.SeededEmployeeAsync();
        var products = await (await employee.Client.GetAsync("/api/products")).ReadAsync<List<Application.Products.ProductResponse>>();
        var yogurt = products.Single(p => p.Name == "Yogurt Fresa");

        var response = await Sell(employee.Client, yogurt.Id, 1m);

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("INVENTORY_EXPIRED", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Post_ShouldRejectInactiveAndUnknownProducts()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.NewEmployeeAsync();
        var inactive = (await (await admin.Client.GetAsync("/api/products?includeInactive=true")).ReadAsync<List<Application.Products.ProductResponse>>())
            .Single(p => p.Name == "Atún en Agua");

        var inactiveResponse = await Sell(employee.Client, inactive.Id, 1m);
        var unknownResponse = await Sell(employee.Client, Guid.NewGuid(), 1m);

        Assert.AreEqual(HttpStatusCode.Conflict, inactiveResponse.StatusCode);
        Assert.AreEqual("PRODUCT_INACTIVE", await inactiveResponse.CodeAsync());
        Assert.AreEqual(HttpStatusCode.NotFound, unknownResponse.StatusCode);
        Assert.AreEqual("PRODUCT_NOT_FOUND", await unknownResponse.CodeAsync());
    }

    [TestMethod]
    public async Task Post_ShouldRejectEmptySalesAndInvalidQuantities()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.NewEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);

        var empty = await employee.Client.PostJsonAsync("/api/sales", new CreateSaleRequest(Array.Empty<CreateSaleItemRequest>()));
        var zero = await Sell(employee.Client, product.Id, 0m);
        var negative = await Sell(employee.Client, product.Id, -3m);

        Assert.AreEqual(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.AreEqual("SALE_EMPTY", await empty.CodeAsync());
        Assert.AreEqual(HttpStatusCode.BadRequest, zero.StatusCode);
        Assert.AreEqual("INVALID_QUANTITY", await zero.CodeAsync());
        Assert.AreEqual(HttpStatusCode.BadRequest, negative.StatusCode);
    }

    [TestMethod]
    public async Task Post_ShouldRejectFractionalQuantitiesForUnitProducts()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.NewEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client, unitType: UnitType.Unit);
        await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);

        var response = await Sell(employee.Client, product.Id, 1.5m);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual("INVALID_QUANTITY", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Post_ShouldNotConsumeAnyStockWhenOneItemFails()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.NewEmployeeAsync();
        var available = await ApiHelpers.CreateProductAsync(admin.Client);
        var stock = await ApiHelpers.CreateStockAsync(admin.Client, available.Id, "L001", 10m);
        var outOfStock = await ApiHelpers.CreateProductAsync(admin.Client);

        var response = await employee.Client.PostJsonAsync(
            "/api/sales",
            new CreateSaleRequest(new[]
            {
                new CreateSaleItemRequest(available.Id, 5m),
                new CreateSaleItemRequest(outOfStock.Id, 1m)
            }));

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        var after = await (await admin.Client.GetAsync($"/api/inventory/{stock.Id}")).ReadAsync<StockResponse>();
        Assert.AreEqual(10m, after.Quantity);
    }

    [TestMethod]
    public async Task Post_ShouldSellSeveralProductsInASingleSale()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.NewEmployeeAsync();
        var first = await ApiHelpers.CreateProductAsync(admin.Client, salePrice: 3200m);
        var second = await ApiHelpers.CreateProductAsync(admin.Client, cost: 3000m, salePrice: 3000m);
        await ApiHelpers.CreateStockAsync(admin.Client, first.Id, "L001", 10m);
        await ApiHelpers.CreateStockAsync(admin.Client, second.Id, "L001", 10m);

        var response = await employee.Client.PostJsonAsync(
            "/api/sales",
            new CreateSaleRequest(new[]
            {
                new CreateSaleItemRequest(first.Id, 4m),
                new CreateSaleItemRequest(second.Id, 1m)
            }));

        var sale = await response.ReadAsync<SaleResponse>();
        Assert.HasCount(2, sale.Items);
        Assert.AreEqual(15800m, sale.Total);
    }

    [TestMethod]
    public async Task ConcurrentSales_ShouldNeverOversell()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client, unitType: UnitType.Unit);
        var stock = await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 3m);
        var buyers = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => ApiHelpers.NewEmployeeAsync()));

        var responses = await Task.WhenAll(buyers.Select(b => Sell(b.Client, product.Id, 3m)));

        Assert.AreEqual(1, responses.Count(r => r.StatusCode == HttpStatusCode.Created));
        Assert.AreEqual(4, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        var after = await (await admin.Client.GetAsync($"/api/inventory/{stock.Id}")).ReadAsync<StockResponse>();
        Assert.AreEqual(0m, after.Quantity);
    }

    [TestMethod]
    public async Task Get_ShouldReturnOnlyTheSalesOfTheCurrentEmployee()
    {
        var admin = await ApiHelpers.AdminAsync();
        var first = await ApiHelpers.NewEmployeeAsync();
        var second = await ApiHelpers.NewEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 20m);
        var firstSale = await (await Sell(first.Client, product.Id, 1m)).ReadAsync<SaleResponse>();
        await Sell(second.Client, product.Id, 1m);

        var firstSales = await (await first.Client.GetAsync("/api/sales")).ReadAsync<List<SaleResponse>>();

        Assert.AreEqual(firstSale.Id, firstSales.Single().Id);
    }

    [TestMethod]
    public async Task Get_ShouldShowTheSeededHistoryToItsOwnerOnly()
    {
        var owner = await ApiHelpers.SeededEmployeeAsync();
        var other = await ApiHelpers.NewEmployeeAsync();

        var ownerSales = await (await owner.Client.GetAsync("/api/sales")).ReadAsync<List<SaleResponse>>();
        var otherSales = await (await other.Client.GetAsync("/api/sales")).ReadAsync<List<SaleResponse>>();

        Assert.IsGreaterThanOrEqualTo(2, ownerSales.Count);
        Assert.IsTrue(ownerSales.All(s => s.UserId == owner.UserId));
        Assert.IsTrue(ownerSales.Any(s => s.Items.Any(i => i.UnitPrice == 5900m)));
        Assert.IsEmpty(otherSales);
    }

    [TestMethod]
    public async Task Get_ShouldLetAdministratorsSeeEverySale()
    {
        var admin = await ApiHelpers.AdminAsync();

        var sales = await (await admin.Client.GetAsync("/api/sales")).ReadAsync<List<SaleResponse>>();

        Assert.IsTrue(sales.Select(s => s.UserId).Distinct().Any());
        Assert.IsGreaterThanOrEqualTo(2, sales.Count);
    }

    [TestMethod]
    public async Task GetById_ShouldHideSalesOfOtherEmployees()
    {
        var admin = await ApiHelpers.AdminAsync();
        var owner = await ApiHelpers.NewEmployeeAsync();
        var intruder = await ApiHelpers.NewEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);
        await ApiHelpers.CreateStockAsync(admin.Client, product.Id, "L001", 10m);
        var sale = await (await Sell(owner.Client, product.Id, 1m)).ReadAsync<SaleResponse>();

        var asOwner = await owner.Client.GetAsync($"/api/sales/{sale.Id}");
        var asIntruder = await intruder.Client.GetAsync($"/api/sales/{sale.Id}");
        var asAdmin = await admin.Client.GetAsync($"/api/sales/{sale.Id}");

        Assert.AreEqual(HttpStatusCode.OK, asOwner.StatusCode);
        Assert.AreEqual(HttpStatusCode.NotFound, asIntruder.StatusCode);
        Assert.AreEqual("SALE_NOT_FOUND", await asIntruder.CodeAsync());
        Assert.AreEqual(HttpStatusCode.OK, asAdmin.StatusCode);
    }
}
