using Application.Common;
using Application.Sales;
using Domain.Entities;
using Domain.Enums;
using Market.Tests.Application.Fakes;

namespace Market.Tests.Application;

[TestClass]
public class SalesQueriesTests
{
    private readonly InMemoryProductRepository _products = new();
    private readonly InMemorySaleRepository _sales = new();
    private readonly FakeCurrentUser _currentUser = new();
    private readonly Product _milk = Product.Create("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m);
    private readonly Guid _otherUser = Guid.NewGuid();

    public SalesQueriesTests() => _products.Items.Add(_milk);

    private Sale AddSale(Guid userId, DateTime date)
    {
        var sale = Sale.Create(userId, date);
        sale.AddItem(_milk.Id, 2m, 3200m);
        sale.Complete();
        _sales.Items.Add(sale);
        return sale;
    }

    [TestMethod]
    public async Task GetSales_ShouldReturnOnlyOwnSalesForEmployees()
    {
        var mine = AddSale(_currentUser.UserId, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        AddSale(_otherUser, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));

        var result = await new GetSalesUseCase(_sales, _products, _currentUser).ExecuteAsync();

        Assert.AreEqual(mine.Id, result.Single().Id);
    }

    [TestMethod]
    public async Task GetSales_ShouldReturnEverySaleForAdministrators()
    {
        _currentUser.Role = UserRole.Administrator;
        AddSale(_currentUser.UserId, new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc));
        AddSale(_otherUser, new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc));

        var result = await new GetSalesUseCase(_sales, _products, _currentUser).ExecuteAsync();

        Assert.HasCount(2, result);
    }

    [TestMethod]
    public async Task GetSaleById_ShouldReturnOwnSaleWithProductNames()
    {
        var mine = AddSale(_currentUser.UserId, DateTime.UtcNow);

        var result = await new GetSaleByIdUseCase(_sales, _products, _currentUser).ExecuteAsync(mine.Id);

        Assert.AreEqual("Leche Entera", result.Items.Single().ProductName);
        Assert.AreEqual(6400m, result.Total);
    }

    [TestMethod]
    public async Task GetSaleById_ShouldHideSalesOfOtherUsersFromEmployees()
    {
        var theirs = AddSale(_otherUser, DateTime.UtcNow);

        var ex = await Assert.ThrowsAsync<AppException>(() =>
            new GetSaleByIdUseCase(_sales, _products, _currentUser).ExecuteAsync(theirs.Id));

        Assert.AreEqual(AppErrorCodes.SaleNotFound, ex.Code);
    }

    [TestMethod]
    public async Task GetSaleById_ShouldAllowAdministratorsToReadAnySale()
    {
        _currentUser.Role = UserRole.Administrator;
        var theirs = AddSale(_otherUser, DateTime.UtcNow);

        var result = await new GetSaleByIdUseCase(_sales, _products, _currentUser).ExecuteAsync(theirs.Id);

        Assert.AreEqual(theirs.Id, result.Id);
    }

    [TestMethod]
    public async Task GetSaleById_ShouldReturnNotFoundForUnknownSale()
    {
        var ex = await Assert.ThrowsAsync<AppException>(() =>
            new GetSaleByIdUseCase(_sales, _products, _currentUser).ExecuteAsync(Guid.NewGuid()));

        Assert.AreEqual(AppErrorCodes.SaleNotFound, ex.Code);
    }
}
