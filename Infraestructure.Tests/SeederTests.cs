using Domain.Enums;
using Infraestructure.Authentication;
using Infraestructure.Persistance;
using Infraestructure.Persistance.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Market.Tests.Infraestructure;

[TestClass]
public class SeederTests
{
    [TestMethod]
    public async Task Seed_ShouldCreateTheDocumentedDataSet()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        await using var scope = host.Scope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        Assert.AreEqual(3, await context.Users.CountAsync());
        Assert.AreEqual(8, await context.Products.CountAsync());
        Assert.AreEqual(8, await context.WarehouseStocks.CountAsync());
        Assert.AreEqual(2, await context.Sales.CountAsync());
        Assert.AreEqual(4, await context.Tasks.CountAsync());
    }

    [TestMethod]
    public async Task Seed_ShouldCreateAdministratorAndEmployeesWithHashedPasswords()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        await using var scope = host.Scope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = new PasswordHasher();

        var admin = await context.Users.SingleAsync(u => u.Email == "admin@supermarket.local");
        var employee = await context.Users.SingleAsync(u => u.Email == "employee1@supermarket.local");

        Assert.AreEqual(UserRole.Administrator, admin.Role);
        Assert.AreEqual(UserRole.Employee, employee.Role);
        Assert.IsTrue(hasher.Verify("Admin123!", admin.PasswordHash));
        Assert.IsTrue(hasher.Verify("Employee123!", employee.PasswordHash));
    }

    [TestMethod]
    public async Task Seed_ShouldCoverTheBusinessScenarios()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        await using var scope = host.Scope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        var lots = await context.WarehouseStocks.AsNoTracking().ToListAsync();
        var milkLots = lots.Where(l => l.BatchNumber is "L001" or "L002" or "L003").ToList();

        Assert.HasCount(3, milkLots);
        Assert.IsTrue(lots.Single(l => l.BatchNumber == "YOG-001").IsExpired(now));
        Assert.IsTrue(lots.Single(l => l.BatchNumber == "ARR-001").IsLowStock());
        Assert.IsFalse(lots.Single(l => l.BatchNumber == "L001").IsExpired(now));
        Assert.AreEqual(3m, lots.Single(l => l.BatchNumber == "HUE-001").Quantity);

        var tuna = await context.Products.AsNoTracking().SingleAsync(p => p.Name == "Atún en Agua");
        Assert.IsFalse(tuna.IsActive);
        Assert.IsFalse(lots.Any(l => l.ProductId == tuna.Id));

        var detergent = await context.Products.AsNoTracking().SingleAsync(p => p.Name == "Detergente Líquido");
        Assert.IsFalse(lots.Any(l => l.ProductId == detergent.Id));

        var cola = await context.Products.AsNoTracking().SingleAsync(p => p.Name == "Gaseosa Cola");
        Assert.AreEqual(cola.Cost, cola.SalePrice);
    }

    [TestMethod]
    public async Task Seed_ShouldKeepHistoricalPricesAndOnlyCompletedSales()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        await using var scope = host.Scope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sales = await context.Sales.AsNoTracking().Include(s => s.Items).OrderBy(s => s.SaleDate).ToListAsync();
        var bread = await context.Products.AsNoTracking().SingleAsync(p => p.Name == "Pan Tajado");

        Assert.IsTrue(sales.All(s => s.Status == SaleStatus.Completed));
        Assert.AreEqual(5900m, sales[0].Items.Single().UnitPrice);
        Assert.AreNotEqual(bread.SalePrice, sales[0].Items.Single().UnitPrice);
        Assert.AreEqual(15800m, sales[1].Total);
    }

    [TestMethod]
    public async Task Seed_ShouldCreateTasksInEveryStatusAndAnotherUsersTask()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        await using var scope = host.Scope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var employee1 = await context.Users.SingleAsync(u => u.Email == "employee1@supermarket.local");
        var employee2 = await context.Users.SingleAsync(u => u.Email == "employee2@supermarket.local");
        var tasks = await context.Tasks.AsNoTracking().ToListAsync();

        Assert.HasCount(3, tasks.Where(t => t.UserId == employee1.Id).ToList());
        Assert.AreEqual(1, tasks.Count(t => t.UserId == employee2.Id));
        Assert.AreEqual(
            3,
            tasks.Where(t => t.UserId == employee1.Id).Select(t => t.Status).Distinct().Count());
    }

    [TestMethod]
    public async Task Seed_ShouldBeIdempotent()
    {
        await using var host = await TestHost.CreateAsync(seed: true);
        await using var scope = host.Scope();

        var seededAgain = await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync();

        Assert.IsFalse(seededAgain);
        Assert.AreEqual(3, await scope.ServiceProvider.GetRequiredService<AppDbContext>().Users.CountAsync());
    }

    [TestMethod]
    public async Task Seed_ShouldRefuseToRunWithoutConfiguredPasswords()
    {
        await using var host = await TestHost.CreateAsync();
        await using var scope = host.Scope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var seeder = new DataSeeder(context, new PasswordHasher(), new SeedOptions(), TimeProvider.System);

        await Assert.ThrowsAsync<InvalidOperationException>(() => seeder.SeedAsync());
    }
}
