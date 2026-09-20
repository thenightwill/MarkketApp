using Application.Common;
using Application.Interfaces.Persistance;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Infraestructure.Persistance;

namespace Market.Tests.Infraestructure;

[TestClass]
public class PersistenceTests
{
    private static readonly DateTime Now = DateTime.UtcNow;

    private static Product NewProduct(string name = "Leche Entera", UnitType type = UnitType.Volume) =>
        Product.Create(name, "Alpina", "Lácteos", type, 1m, 2500m, 3200m);

    [TestMethod]
    public async Task Product_ShouldRoundTripThroughTheDatabase()
    {
        await using var host = await TestHost.CreateAsync();
        var product = NewProduct();

        await using (var scope = host.Scope())
        {
            await scope.ServiceProvider.GetRequiredService<IProductRepository>().AddAsync(product);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using (var scope = host.Scope())
        {
            var loaded = await scope.ServiceProvider.GetRequiredService<IProductRepository>().GetByIdAsync(product.Id);

            Assert.IsNotNull(loaded);
            Assert.AreEqual("Leche Entera", loaded.Name);
            Assert.AreEqual(UnitType.Volume, loaded.UnitType);
            Assert.AreEqual(3200m, loaded.SalePrice);
            Assert.IsTrue(loaded.IsActive);
        }
    }

    [TestMethod]
    public async Task Product_ShouldDetectActiveDuplicatesIgnoringCase()
    {
        await using var host = await TestHost.CreateAsync();
        await using var scope = host.Scope();
        var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var product = NewProduct();
        await repo.AddAsync(product);
        await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        var duplicate = await repo.ExistsActiveDuplicateAsync("LECHE ENTERA", "alpina", "lácteos", UnitType.Volume, 1m, null);
        var self = await repo.ExistsActiveDuplicateAsync("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, product.Id);
        var differentUnit = await repo.ExistsActiveDuplicateAsync("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 0.5m, null);

        Assert.IsTrue(duplicate);
        Assert.IsFalse(self);
        Assert.IsFalse(differentUnit);
    }

    [TestMethod]
    public async Task Product_UniqueIndexShouldBlockActiveDuplicatesAndReportDuplicateProduct()
    {
        await using var host = await TestHost.CreateAsync();
        await using var scope = host.Scope();
        var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await repo.AddAsync(NewProduct());
        await uow.SaveChangesAsync();

        await repo.AddAsync(NewProduct());
        var ex = await Assert.ThrowsAsync<AppException>(() => uow.SaveChangesAsync());

        Assert.AreEqual(AppErrorCodes.DuplicateProduct, ex.Code);
    }

    [TestMethod]
    public async Task Product_UniqueIndexShouldAllowANewActiveProductAfterDeactivation()
    {
        await using var host = await TestHost.CreateAsync();
        await using var scope = host.Scope();
        var repo = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var first = NewProduct();
        await repo.AddAsync(first);
        await uow.SaveChangesAsync();

        first.Deactivate();
        await repo.AddAsync(NewProduct());
        await uow.SaveChangesAsync();

        Assert.HasCount(2, await repo.ListAsync(includeInactive: true));
        Assert.HasCount(1, await repo.ListAsync(includeInactive: false));
    }

    [TestMethod]
    public async Task Stock_ShouldRejectDuplicatedBatchForTheSameProductThroughTheUniqueIndex()
    {
        await using var host = await TestHost.CreateAsync();
        await using var scope = host.Scope();
        var products = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var stocks = scope.ServiceProvider.GetRequiredService<IWarehouseStockRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var product = NewProduct();
        await products.AddAsync(product);
        await stocks.AddAsync(WarehouseStock.Create(product.Id, "L001", "A-01", 10m, 2m, Now.AddDays(-1), Now.AddDays(30)));
        await uow.SaveChangesAsync();

        Assert.IsTrue(await stocks.BatchExistsAsync(product.Id, "L001", null));
        Assert.IsFalse(await stocks.BatchExistsAsync(product.Id, "L002", null));

        await stocks.AddAsync(WarehouseStock.Create(product.Id, "L001", "A-02", 5m, 2m, Now.AddDays(-1), Now.AddDays(30)));
        var ex = await Assert.ThrowsAsync<AppException>(() => uow.SaveChangesAsync());

        Assert.AreEqual(AppErrorCodes.DuplicateBatch, ex.Code);
    }

    [TestMethod]
    public async Task Stock_SellableQueryShouldReturnOnlyActiveLotsWithQuantity()
    {
        await using var host = await TestHost.CreateAsync();
        await using var scope = host.Scope();
        var products = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var stocks = scope.ServiceProvider.GetRequiredService<IWarehouseStockRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var product = NewProduct();
        await products.AddAsync(product);
        var active = WarehouseStock.Create(product.Id, "A", "A-01", 10m, 2m, Now.AddDays(-1), Now.AddDays(30));
        var inactive = WarehouseStock.Create(product.Id, "B", "A-01", 10m, 2m, Now.AddDays(-1), Now.AddDays(30));
        inactive.Deactivate();
        var empty = WarehouseStock.Create(product.Id, "C", "A-01", 10m, 2m, Now.AddDays(-1), Now.AddDays(30));
        empty.RemoveQuantity(10m);
        await stocks.AddAsync(active);
        await stocks.AddAsync(inactive);
        await stocks.AddAsync(empty);
        await uow.SaveChangesAsync();

        var sellable = await stocks.GetSellableByProductIdsAsync(new[] { product.Id });

        Assert.AreEqual("A", sellable.Single().BatchNumber);
    }

    [TestMethod]
    public async Task User_ShouldEnforceUniqueEmail()
    {
        await using var host = await TestHost.CreateAsync();
        await using var scope = host.Scope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await users.AddAsync(User.Create("Ana", "ana@supermarket.local", "hash", UserRole.Employee));
        await uow.SaveChangesAsync();

        Assert.IsTrue(await users.EmailExistsAsync("ANA@supermarket.local"));
        Assert.IsNotNull(await users.GetByEmailAsync(" Ana@Supermarket.local "));

        await users.AddAsync(User.Create("Otra", "ana@supermarket.local", "hash", UserRole.Employee));
        var ex = await Assert.ThrowsAsync<AppException>(() => uow.SaveChangesAsync());

        Assert.AreEqual(AppErrorCodes.EmailAlreadyExists, ex.Code);
    }

    [TestMethod]
    public async Task Task_RepositoryShouldNeverReturnTasksOfAnotherUser()
    {
        await using var host = await TestHost.CreateAsync();
        await using var scope = host.Scope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var tasks = scope.ServiceProvider.GetRequiredService<IUserTaskRepository>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var ana = User.Create("Ana", "ana@supermarket.local", "hash", UserRole.Employee);
        var luis = User.Create("Luis", "luis@supermarket.local", "hash", UserRole.Employee);
        await users.AddAsync(ana);
        await users.AddAsync(luis);
        var anasTask = UserTask.Create(ana.Id, "De Ana", null, Now.AddDays(1));
        await tasks.AddAsync(anasTask);
        await tasks.AddAsync(UserTask.Create(luis.Id, "De Luis", null, Now.AddDays(1)));
        await uow.SaveChangesAsync();

        var own = await tasks.ListByUserAsync(ana.Id);
        var foreign = await tasks.GetByIdAsync(anasTask.Id, luis.Id);
        var mine = await tasks.GetByIdAsync(anasTask.Id, ana.Id);

        Assert.AreEqual("De Ana", own.Single().Title);
        Assert.IsNull(foreign);
        Assert.IsNotNull(mine);
    }

    [TestMethod]
    public async Task Task_ShouldPersistStatusAndRemoveOnDelete()
    {
        await using var host = await TestHost.CreateAsync();
        var ana = User.Create("Ana", "ana@supermarket.local", "hash", UserRole.Employee);
        var task = UserTask.Create(ana.Id, "De Ana", "desc", Now.AddDays(1));

        await using (var scope = host.Scope())
        {
            await scope.ServiceProvider.GetRequiredService<IUserRepository>().AddAsync(ana);
            await scope.ServiceProvider.GetRequiredService<IUserTaskRepository>().AddAsync(task);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using (var scope = host.Scope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IUserTaskRepository>();
            var loaded = (await repo.GetByIdAsync(task.Id, ana.Id))!;
            loaded.Start();
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using (var scope = host.Scope())
        {
            var repo = scope.ServiceProvider.GetRequiredService<IUserTaskRepository>();
            var loaded = (await repo.GetByIdAsync(task.Id, ana.Id))!;
            Assert.AreEqual(UserTaskStatus.InProgress, loaded.Status);
            repo.Remove(loaded);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using (var scope = host.Scope())
        {
            Assert.IsNull(await scope.ServiceProvider.GetRequiredService<IUserTaskRepository>().GetByIdAsync(task.Id, ana.Id));
        }
    }

    [TestMethod]
    public async Task Sale_ShouldPersistItemsAndHistoricalPrices()
    {
        await using var host = await TestHost.CreateAsync();
        var ana = User.Create("Ana", "ana@supermarket.local", "hash", UserRole.Employee);
        var product = NewProduct();
        var sale = Sale.Create(ana.Id, Now);
        sale.AddItem(product.Id, 3m, 3200m);
        sale.Complete();

        await using (var scope = host.Scope())
        {
            await scope.ServiceProvider.GetRequiredService<IUserRepository>().AddAsync(ana);
            await scope.ServiceProvider.GetRequiredService<IProductRepository>().AddAsync(product);
            await scope.ServiceProvider.GetRequiredService<ISaleRepository>().AddAsync(sale);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using (var scope = host.Scope())
        {
            var loaded = (await scope.ServiceProvider.GetRequiredService<ISaleRepository>().GetByIdAsync(sale.Id))!;

            Assert.AreEqual(9600m, loaded.Total);
            Assert.AreEqual(SaleStatus.Completed, loaded.Status);
            Assert.AreEqual(3200m, loaded.Items.Single().UnitPrice);
            Assert.AreEqual(9600m, loaded.Items.Single().Subtotal);
        }
    }

    [TestMethod]
    public async Task Dates_ShouldComeBackAsUtc()
    {
        await using var host = await TestHost.CreateAsync();
        var product = NewProduct();
        var stock = WarehouseStock.Create(product.Id, "L001", "A-01", 10m, 2m, Now.AddDays(-1), Now.AddDays(30));

        await using (var scope = host.Scope())
        {
            await scope.ServiceProvider.GetRequiredService<IProductRepository>().AddAsync(product);
            await scope.ServiceProvider.GetRequiredService<IWarehouseStockRepository>().AddAsync(stock);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using (var scope = host.Scope())
        {
            var loaded = (await scope.ServiceProvider.GetRequiredService<IWarehouseStockRepository>().GetByIdAsync(stock.Id))!;

            Assert.AreEqual(DateTimeKind.Utc, loaded.ExpirationDate.Kind);
            Assert.AreEqual(DateTimeKind.Utc, loaded.CreatedAt.Kind);
        }
    }

    [TestMethod]
    public async Task Stock_ShouldDetectConcurrentModificationsWithRowVersion()
    {
        await using var host = await TestHost.CreateAsync();
        var product = NewProduct();
        var stock = WarehouseStock.Create(product.Id, "L001", "A-01", 10m, 2m, Now.AddDays(-1), Now.AddDays(30));

        await using (var scope = host.Scope())
        {
            await scope.ServiceProvider.GetRequiredService<IProductRepository>().AddAsync(product);
            await scope.ServiceProvider.GetRequiredService<IWarehouseStockRepository>().AddAsync(stock);
            await scope.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();
        }

        await using var scopeA = host.Scope();
        await using var scopeB = host.Scope();
        var lotA = (await scopeA.ServiceProvider.GetRequiredService<IWarehouseStockRepository>().GetByIdAsync(stock.Id))!;
        var lotB = (await scopeB.ServiceProvider.GetRequiredService<IWarehouseStockRepository>().GetByIdAsync(stock.Id))!;

        lotA.RemoveQuantity(6m);
        await scopeA.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync();

        lotB.RemoveQuantity(6m);
        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            scopeB.ServiceProvider.GetRequiredService<IUnitOfWork>().SaveChangesAsync());
    }

    [TestMethod]
    public async Task Transaction_ShouldRollBackEverythingWhenTheActionFails()
    {
        await using var host = await TestHost.CreateAsync();
        var product = NewProduct();

        await using (var scope = host.Scope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await Assert.ThrowsAsync<InvalidOperationException>(() => uow.ExecuteInTransactionAsync<int>(async () =>
            {
                await scope.ServiceProvider.GetRequiredService<IProductRepository>().AddAsync(product);
                await uow.SaveChangesAsync();
                throw new InvalidOperationException("boom");
            }));
        }

        await using (var scope = host.Scope())
        {
            Assert.IsNull(await scope.ServiceProvider.GetRequiredService<IProductRepository>().GetByIdAsync(product.Id));
        }
    }

    [TestMethod]
    public async Task Transaction_ShouldCommitWhenTheActionSucceeds()
    {
        await using var host = await TestHost.CreateAsync();
        var product = NewProduct();

        await using (var scope = host.Scope())
        {
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var result = await uow.ExecuteInTransactionAsync(async () =>
            {
                await scope.ServiceProvider.GetRequiredService<IProductRepository>().AddAsync(product);
                await uow.SaveChangesAsync();
                return 42;
            });

            Assert.AreEqual(42, result);
        }

        await using (var scope = host.Scope())
        {
            Assert.IsNotNull(await scope.ServiceProvider.GetRequiredService<IProductRepository>().GetByIdAsync(product.Id));
        }
    }

    [TestMethod]
    public async Task Database_ShouldRejectNegativeQuantitiesThroughTheCheckConstraint()
    {
        await using var host = await TestHost.CreateAsync();
        var product = NewProduct();
        var stock = WarehouseStock.Create(product.Id, "L001", "A-01", 10m, 2m, Now.AddDays(-1), Now.AddDays(30));

        await using var scope = host.Scope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        context.Products.Add(product);
        context.WarehouseStocks.Add(stock);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<SqlException>(() =>
            context.Database.ExecuteSqlRawAsync("UPDATE WarehouseStocks SET Quantity = -1"));
    }
}
