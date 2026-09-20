using Application.Common;
using Application.Products;
using Domain.Enums;
using Domain.Exceptions;
using Market.Tests.Application.Fakes;

namespace Market.Tests.Application;

[TestClass]
public class ProductUseCasesTests
{
    private readonly InMemoryProductRepository _products = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeCurrentUser _currentUser = new();

    private static CreateProductRequest MilkRequest(decimal salePrice = 3200m) =>
        new("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, salePrice);

    private CreateProductUseCase Create() => new(_products, _uow);

    private UpdateProductUseCase Update() => new(_products, _uow);

    private DeactivateProductUseCase Deactivate() => new(_products, _uow);

    private GetProductsUseCase Query() => new(_products, _currentUser);

    private GetProductByIdUseCase QueryById() => new(_products, _currentUser);

    [TestMethod]
    public async Task Create_ShouldPersistProduct()
    {
        var response = await Create().ExecuteAsync(MilkRequest());

        var product = _products.Items.Single();
        Assert.AreEqual(product.Id, response.Id);
        Assert.AreEqual("Leche Entera", response.Name);
        Assert.AreEqual(3200m, response.SalePrice);
        Assert.IsTrue(response.IsActive);
        Assert.AreEqual(1, _uow.SaveCount);
    }

    [TestMethod]
    public async Task Create_ShouldRejectDuplicatedActiveProductIgnoringCaseAndSpaces()
    {
        await Create().ExecuteAsync(MilkRequest());

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            Create().ExecuteAsync(new CreateProductRequest(" leche entera ", "ALPINA", "lácteos", UnitType.Volume, 1m, 2000m, 3000m)));

        Assert.AreEqual(AppErrorCodes.DuplicateProduct, ex.Code);
        Assert.AreEqual(AppErrorKind.Conflict, ex.Kind);
        Assert.HasCount(1, _products.Items);
    }

    [TestMethod]
    public async Task Create_ShouldAllowSameNameWhenUnitValueDiffers()
    {
        await Create().ExecuteAsync(MilkRequest());

        await Create().ExecuteAsync(new CreateProductRequest("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 0.5m, 1500m, 2000m));

        Assert.HasCount(2, _products.Items);
    }

    [TestMethod]
    public async Task Create_ShouldAllowRecreatingAProductAfterItWasDeactivated()
    {
        var first = await Create().ExecuteAsync(MilkRequest());
        await Deactivate().ExecuteAsync(first.Id);

        await Create().ExecuteAsync(MilkRequest());

        Assert.HasCount(2, _products.Items);
    }

    [TestMethod]
    public async Task Create_ShouldPropagateDomainValidation()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => Create().ExecuteAsync(MilkRequest(salePrice: 1000m)));

        Assert.AreEqual(DomainErrorCodes.InvalidProductPrice, ex.Code);
        Assert.IsEmpty(_products.Items);
        Assert.AreEqual(0, _uow.SaveCount);
    }

    [TestMethod]
    public async Task Update_ShouldChangeInformationAndPricing()
    {
        var created = await Create().ExecuteAsync(MilkRequest());

        var response = await Update().ExecuteAsync(
            created.Id,
            new UpdateProductRequest("Leche Deslactosada", "Alpina", "Lácteos", UnitType.Volume, 1m, 2600m, 3400m, true));

        Assert.AreEqual("Leche Deslactosada", response.Name);
        Assert.AreEqual(2600m, response.Cost!.Value);
        Assert.AreEqual(3400m, response.SalePrice);
        Assert.IsNotNull(response.UpdatedAt);
    }

    [TestMethod]
    public async Task Update_ShouldNotChangeAnythingWhenPricingIsInvalid()
    {
        var created = await Create().ExecuteAsync(MilkRequest());

        await Assert.ThrowsAsync<DomainException>(() => Update().ExecuteAsync(
            created.Id,
            new UpdateProductRequest("Leche Deslactosada", "Alpina", "Lácteos", UnitType.Volume, 1m, 5000m, 3400m, true)));

        var product = _products.Items.Single();
        Assert.AreEqual("Leche Entera", product.Name);
        Assert.AreEqual(2500m, product.Cost);
    }

    [TestMethod]
    public async Task Update_ShouldReturnNotFoundForUnknownProduct()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Update().ExecuteAsync(
            Guid.NewGuid(),
            new UpdateProductRequest("Leche", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m, true)));

        Assert.AreEqual(AppErrorCodes.ProductNotFound, ex.Code);
        Assert.AreEqual(AppErrorKind.NotFound, ex.Kind);
    }

    [TestMethod]
    public async Task Update_ShouldRejectChangingIntoAnExistingActiveProduct()
    {
        await Create().ExecuteAsync(MilkRequest());
        var other = await Create().ExecuteAsync(new CreateProductRequest("Yogurt", "Alpina", "Lácteos", UnitType.Volume, 1m, 1000m, 1500m));

        var ex = await Assert.ThrowsAsync<AppException>(() => Update().ExecuteAsync(
            other.Id,
            new UpdateProductRequest("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 1000m, 1500m, true)));

        Assert.AreEqual(AppErrorCodes.DuplicateProduct, ex.Code);
    }

    [TestMethod]
    public async Task Update_ShouldAllowSavingTheSameProductWithoutSelfDuplicateError()
    {
        var created = await Create().ExecuteAsync(MilkRequest());

        var response = await Update().ExecuteAsync(
            created.Id,
            new UpdateProductRequest("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3300m, true));

        Assert.AreEqual(3300m, response.SalePrice);
    }

    [TestMethod]
    public async Task Update_ShouldDeactivateProductWhenRequested()
    {
        var created = await Create().ExecuteAsync(MilkRequest());

        var response = await Update().ExecuteAsync(
            created.Id,
            new UpdateProductRequest("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m, false));

        Assert.IsFalse(response.IsActive);
    }

    [TestMethod]
    public async Task Update_ShouldRejectReactivatingWhenAnotherActiveDuplicateExists()
    {
        var first = await Create().ExecuteAsync(MilkRequest());
        await Deactivate().ExecuteAsync(first.Id);
        await Create().ExecuteAsync(MilkRequest());

        var ex = await Assert.ThrowsAsync<AppException>(() => Update().ExecuteAsync(
            first.Id,
            new UpdateProductRequest("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m, true)));

        Assert.AreEqual(AppErrorCodes.DuplicateProduct, ex.Code);
        Assert.IsFalse(_products.Items.First(p => p.Id == first.Id).IsActive);
    }

    [TestMethod]
    public async Task Deactivate_ShouldSoftDeleteTheProduct()
    {
        var created = await Create().ExecuteAsync(MilkRequest());

        await Deactivate().ExecuteAsync(created.Id);

        Assert.HasCount(1, _products.Items);
        Assert.IsFalse(_products.Items.Single().IsActive);
    }

    [TestMethod]
    public async Task Deactivate_ShouldReturnNotFoundForUnknownProduct()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() => Deactivate().ExecuteAsync(Guid.NewGuid()));

        Assert.AreEqual(AppErrorCodes.ProductNotFound, ex.Code);
    }

    [TestMethod]
    public async Task GetProducts_ShouldHideInactiveProductsUnlessRequested()
    {
        var milk = await Create().ExecuteAsync(MilkRequest());
        await Create().ExecuteAsync(new CreateProductRequest("Yogurt", "Alpina", "Lácteos", UnitType.Volume, 1m, 1000m, 1500m));
        await Deactivate().ExecuteAsync(milk.Id);
        var query = Query();

        var active = await query.ExecuteAsync(includeInactive: false);
        var all = await query.ExecuteAsync(includeInactive: true);

        Assert.HasCount(1, active);
        Assert.HasCount(2, all);
    }

    [TestMethod]
    public async Task GetProducts_ShouldHideCostFromEmployees()
    {
        await Create().ExecuteAsync(MilkRequest());
        _currentUser.Role = UserRole.Employee;

        var products = await Query().ExecuteAsync(includeInactive: false);

        Assert.IsNull(products.Single().Cost);
    }

    [TestMethod]
    public async Task GetProducts_ShouldShowCostToAdministrators()
    {
        await Create().ExecuteAsync(MilkRequest());
        _currentUser.Role = UserRole.Administrator;

        var products = await Query().ExecuteAsync(includeInactive: false);

        Assert.AreEqual(2500m, products.Single().Cost!.Value);
    }

    [TestMethod]
    public async Task GetProductById_ShouldReturnProductOrNotFound()
    {
        var created = await Create().ExecuteAsync(MilkRequest());
        var query = QueryById();

        var response = await query.ExecuteAsync(created.Id);
        var ex = await Assert.ThrowsAsync<AppException>(() => query.ExecuteAsync(Guid.NewGuid()));

        Assert.AreEqual(created.Id, response.Id);
        Assert.AreEqual(AppErrorCodes.ProductNotFound, ex.Code);
    }

    [TestMethod]
    public async Task GetProductById_ShouldHideCostFromEmployeesAndShowItToAdministrators()
    {
        var created = await Create().ExecuteAsync(MilkRequest());
        _currentUser.Role = UserRole.Employee;

        var asEmployee = await QueryById().ExecuteAsync(created.Id);

        _currentUser.Role = UserRole.Administrator;
        var asAdmin = await QueryById().ExecuteAsync(created.Id);

        Assert.IsNull(asEmployee.Cost);
        Assert.AreEqual(2500m, asAdmin.Cost!.Value);
    }
}
