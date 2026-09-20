using Application.Common;
using Application.Interfaces.Persistance;
using Application.Sales;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Infraestructure.Persistance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Market.Tests.Infraestructure;

[TestClass]
public class SalesIntegrationTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private static async Task<(Guid UserId, Product Product, Guid StockId)> ArrangeAsync(TestHost host, decimal quantity)
    {
        var user = User.Create("Ana", "ana@supermarket.local", "hash", UserRole.Employee);
        var product = Product.Create("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m);
        var stock = WarehouseStock.Create(product.Id, "L001", "A-01", quantity, 1m, Now.AddDays(-5), Now.AddDays(30));

        await using var scope = host.Scope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Users.Add(user);
        context.Products.Add(product);
        context.WarehouseStocks.Add(stock);
        await context.SaveChangesAsync();

        host.CurrentUser.UserId = user.Id;
        return (user.Id, product, stock.Id);
    }

    private static async Task<SaleResponse> SellAsync(TestHost host, Guid productId, decimal quantity)
    {
        await using var scope = host.Scope();
        return await scope.ServiceProvider
            .GetRequiredService<CreateSaleUseCase>()
            .ExecuteAsync(new CreateSaleRequest(new[] { new CreateSaleItemRequest(productId, quantity) }));
    }

    private static async Task<decimal> QuantityAsync(TestHost host, Guid stockId)
    {
        await using var scope = host.Scope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.WarehouseStocks.AsNoTracking().Where(s => s.Id == stockId).Select(s => s.Quantity).SingleAsync();
    }

    private static async Task<int> SalesCountAsync(TestHost host)
    {
        await using var scope = host.Scope();
        return await scope.ServiceProvider.GetRequiredService<AppDbContext>().Sales.CountAsync();
    }

    [TestMethod]
    public async Task CreateSale_ShouldPersistSaleAndDecreaseStockTogether()
    {
        await using var host = await TestHost.CreateAsync();
        var (userId, product, stockId) = await ArrangeAsync(host, 10m);

        var response = await SellAsync(host, product.Id, 4m);

        Assert.AreEqual(12800m, response.Total);
        Assert.AreEqual(userId, response.UserId);
        Assert.AreEqual(6m, await QuantityAsync(host, stockId));
        Assert.AreEqual(1, await SalesCountAsync(host));
    }

    [TestMethod]
    public async Task CreateSale_ShouldApplyFefoAcrossRealBatches()
    {
        await using var host = await TestHost.CreateAsync();
        var (_, product, middleId) = await ArrangeAsync(host, 30m);
        Guid earliestId, latestId;

        await using (var scope = host.Scope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var early = WarehouseStock.Create(product.Id, "L000", "A-01", 10m, 1m, Now.AddDays(-20), Now.AddDays(12));
            var late = WarehouseStock.Create(product.Id, "L002", "A-01", 20m, 1m, Now.AddDays(-10), Now.AddDays(42));
            context.WarehouseStocks.AddRange(early, late);
            await context.SaveChangesAsync();
            earliestId = early.Id;
            latestId = late.Id;
        }

        await SellAsync(host, product.Id, 15m);

        Assert.AreEqual(0m, await QuantityAsync(host, earliestId));
        Assert.AreEqual(25m, await QuantityAsync(host, middleId));
        Assert.AreEqual(20m, await QuantityAsync(host, latestId));
    }

    [TestMethod]
    public async Task CreateSale_ShouldLeaveNothingBehindWhenStockIsInsufficient()
    {
        await using var host = await TestHost.CreateAsync();
        var (_, product, stockId) = await ArrangeAsync(host, 10m);

        var ex = await Assert.ThrowsAsync<DomainException>(() => SellAsync(host, product.Id, 11m));

        Assert.AreEqual(DomainErrorCodes.InsufficientStock, ex.Code);
        Assert.AreEqual(10m, await QuantityAsync(host, stockId));
        Assert.AreEqual(0, await SalesCountAsync(host));
    }

    [TestMethod]
    public async Task CreateSale_ConcurrentSalesMustNeverOversellTheSameStock()
    {
        await using var host = await TestHost.CreateAsync();
        var (_, product, stockId) = await ArrangeAsync(host, 10m);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 2).Select(async _ =>
            {
                try
                {
                    await SellAsync(host, product.Id, 6m);
                    return (Exception?)null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            }));

        var failures = results.Where(r => r is not null).ToList();
        Assert.HasCount(1, failures);
        Assert.AreEqual(DomainErrorCodes.InsufficientStock, ((DomainException)failures[0]!).Code);
        Assert.AreEqual(4m, await QuantityAsync(host, stockId));
        Assert.AreEqual(1, await SalesCountAsync(host));
    }

    [TestMethod]
    public async Task CreateSale_ManyConcurrentSalesShouldKeepStockConsistent()
    {
        await using var host = await TestHost.CreateAsync();
        var (_, product, stockId) = await ArrangeAsync(host, 10m);

        var results = await Task.WhenAll(
            Enumerable.Range(0, 6).Select(async _ =>
            {
                try
                {
                    await SellAsync(host, product.Id, 3m);
                    return true;
                }
                catch (Exception ex) when (ex is DomainException or AppException)
                {
                    return false;
                }
            }));

        var successes = results.Count(r => r);
        var remaining = await QuantityAsync(host, stockId);

        Assert.IsGreaterThanOrEqualTo(1, successes);
        Assert.IsLessThanOrEqualTo(3, successes);
        Assert.AreEqual(10m - (successes * 3m), remaining);
        Assert.IsGreaterThanOrEqualTo(0m, remaining);
        Assert.AreEqual(successes, await SalesCountAsync(host));
    }

    [TestMethod]
    public async Task CreateSale_ShouldNotSellExpiredStock()
    {
        await using var host = await TestHost.CreateAsync();
        var (_, product, stockId) = await ArrangeAsync(host, 10m);

        await using (var scope = host.Scope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var stock = await context.WarehouseStocks.SingleAsync(s => s.Id == stockId);
            stock.UpdateDetails("A-01", 1m, Now.AddDays(-1));
            await context.SaveChangesAsync();
        }

        var ex = await Assert.ThrowsAsync<DomainException>(() => SellAsync(host, product.Id, 1m));

        Assert.AreEqual(DomainErrorCodes.InventoryExpired, ex.Code);
    }
}
