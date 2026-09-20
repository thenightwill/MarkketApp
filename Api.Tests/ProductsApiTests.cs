using System.Net;
using Application.Products;
using Domain.Enums;

namespace Market.Tests.Api;

[TestClass]
public class ProductsApiTests
{
    [TestMethod]
    public async Task Get_ShouldHideInactiveProductsFromEmployees()
    {
        var employee = await ApiHelpers.SeededEmployeeAsync();

        var response = await employee.Client.GetAsync("/api/products?includeInactive=true");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var products = await response.ReadAsync<List<ProductResponse>>();
        Assert.IsTrue(products.All(p => p.IsActive));
        Assert.IsFalse(products.Any(p => p.Name == "Atún en Agua"));
        Assert.IsTrue(products.All(p => p.Cost is null));
    }

    [TestMethod]
    public async Task Get_ShouldLetAdministratorsSeeInactiveProducts()
    {
        var admin = await ApiHelpers.AdminAsync();

        var products = await (await admin.Client.GetAsync("/api/products?includeInactive=true")).ReadAsync<List<ProductResponse>>();

        Assert.IsTrue(products.Any(p => p.Name == "Atún en Agua" && !p.IsActive));
    }

    [TestMethod]
    public async Task Post_ShouldBeForbiddenForEmployees()
    {
        var employee = await ApiHelpers.SeededEmployeeAsync();

        var response = await employee.Client.PostJsonAsync(
            "/api/products",
            new CreateProductRequest("Nuevo", "Marca", "Cat", UnitType.Unit, 1m, 10m, 20m));

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.AreEqual("FORBIDDEN", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Post_ShouldCreateAProductAndReturnItsLocation()
    {
        var admin = await ApiHelpers.AdminAsync();

        var response = await admin.Client.PostJsonAsync(
            "/api/products",
            new CreateProductRequest($"Cafe {Guid.NewGuid():N}", "Juan Valdez", "Bebidas", UnitType.Weight, 0.5m, 8000m, 11000m));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        var product = await response.ReadAsync<ProductResponse>();
        Assert.IsNotNull(response.Headers.Location);
        Assert.EndsWith($"/api/products/{product.Id}", response.Headers.Location!.AbsolutePath);
        Assert.AreEqual("Weight", product.UnitType);
        Assert.IsTrue(product.IsActive);
    }

    [TestMethod]
    public async Task Post_ShouldRejectDuplicatedActiveProducts()
    {
        var admin = await ApiHelpers.AdminAsync();
        var request = new CreateProductRequest($"Dup {Guid.NewGuid():N}", "Marca", "Cat", UnitType.Unit, 1m, 10m, 20m);
        await admin.Client.PostJsonAsync("/api/products", request);

        var response = await admin.Client.PostJsonAsync("/api/products", request with { Name = request.Name.ToUpperInvariant() });

        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
        Assert.AreEqual("DUPLICATE_PRODUCT", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Post_ShouldRejectSalePriceLowerThanCost()
    {
        var admin = await ApiHelpers.AdminAsync();

        var response = await admin.Client.PostJsonAsync(
            "/api/products",
            new CreateProductRequest($"Bad {Guid.NewGuid():N}", "Marca", "Cat", UnitType.Unit, 1m, 2500m, 2000m));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual("INVALID_PRODUCT_PRICE", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Post_ShouldRejectMissingRequiredFields()
    {
        var admin = await ApiHelpers.AdminAsync();

        var response = await admin.Client.PostJsonAsync("/api/products", new { brand = "Marca", category = "Cat", unitType = "Unit", unitValue = 1, cost = 1, salePrice = 2 });

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.AreEqual("VALIDATION_ERROR", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Post_ShouldRejectTooLongNames()
    {
        var admin = await ApiHelpers.AdminAsync();

        var response = await admin.Client.PostJsonAsync(
            "/api/products",
            new CreateProductRequest(new string('x', 201), "Marca", "Cat", UnitType.Unit, 1m, 10m, 20m));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task Put_ShouldUpdateTheProduct()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);

        var response = await admin.Client.PutJsonAsync(
            $"/api/products/{product.Id}",
            new UpdateProductRequest($"{product.Name} v2", "Marca", "Categoria", UnitType.Volume, 1m, 2600m, 3500m, true));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.ReadAsync<ProductResponse>();
        Assert.AreEqual(3500m, updated.SalePrice);
        Assert.AreEqual(2600m, updated.Cost);
        Assert.IsNotNull(updated.UpdatedAt);
    }

    [TestMethod]
    public async Task Put_ShouldReturnNotFoundForUnknownProducts()
    {
        var admin = await ApiHelpers.AdminAsync();

        var response = await admin.Client.PutJsonAsync(
            $"/api/products/{Guid.NewGuid()}",
            new UpdateProductRequest("X", "Marca", "Cat", UnitType.Unit, 1m, 1m, 2m, true));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual("PRODUCT_NOT_FOUND", await response.CodeAsync());
    }

    [TestMethod]
    public async Task Delete_ShouldDeactivateInsteadOfRemoving()
    {
        var admin = await ApiHelpers.AdminAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);

        var delete = await admin.Client.DeleteAsync($"/api/products/{product.Id}");
        var afterwards = await (await admin.Client.GetAsync($"/api/products/{product.Id}")).ReadAsync<ProductResponse>();

        Assert.AreEqual(HttpStatusCode.NoContent, delete.StatusCode);
        Assert.IsFalse(afterwards.IsActive);
    }

    [TestMethod]
    public async Task Delete_ShouldBeForbiddenForEmployees()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.SeededEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client);

        var response = await employee.Client.DeleteAsync($"/api/products/{product.Id}");

        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [TestMethod]
    public async Task Delete_ShouldReturnNotFoundForUnknownProducts()
    {
        var admin = await ApiHelpers.AdminAsync();

        var response = await admin.Client.DeleteAsync($"/api/products/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task GetById_ShouldReturnNotFoundForUnknownProducts()
    {
        var employee = await ApiHelpers.SeededEmployeeAsync();

        var response = await employee.Client.GetAsync($"/api/products/{Guid.NewGuid()}");

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.AreEqual("PRODUCT_NOT_FOUND", await response.CodeAsync());
    }

    [TestMethod]
    public async Task GetById_ShouldHideCostFromEmployeesAndShowItToAdministrators()
    {
        var admin = await ApiHelpers.AdminAsync();
        var employee = await ApiHelpers.SeededEmployeeAsync();
        var product = await ApiHelpers.CreateProductAsync(admin.Client, cost: 2500m);

        var asEmployee = await (await employee.Client.GetAsync($"/api/products/{product.Id}")).ReadAsync<ProductResponse>();
        var asAdmin = await (await admin.Client.GetAsync($"/api/products/{product.Id}")).ReadAsync<ProductResponse>();

        Assert.IsNull(asEmployee.Cost);
        Assert.AreEqual(2500m, asAdmin.Cost!.Value);
    }
}
