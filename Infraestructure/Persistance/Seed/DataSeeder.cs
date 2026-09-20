using Application.Interfaces.Authentication;
using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infraestructure.Persistance.Seed;

public class DataSeeder
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly SeedOptions _options;
    private readonly TimeProvider _time;

    public DataSeeder(AppDbContext context, IPasswordHasher hasher, SeedOptions options, TimeProvider time)
    {
        _context = context;
        _hasher = hasher;
        _options = options;
        _time = time;
    }

    public async Task<bool> SeedAsync(CancellationToken cancellationToken = default)
    {
        if (await _context.Users.AnyAsync(cancellationToken))
            return false;

        if (string.IsNullOrWhiteSpace(_options.AdminPassword) || string.IsNullOrWhiteSpace(_options.EmployeePassword))
            throw new InvalidOperationException("Seed:AdminPassword and Seed:EmployeePassword must be configured to seed the database.");

        var now = _time.GetUtcNow().UtcDateTime;

        var admin = User.Create("Administrator", "admin@supermarket.local", _hasher.Hash(_options.AdminPassword), UserRole.Administrator);
        var employee1 = User.Create("Employee One", "employee1@supermarket.local", _hasher.Hash(_options.EmployeePassword), UserRole.Employee);
        var employee2 = User.Create("Employee Two", "employee2@supermarket.local", _hasher.Hash(_options.EmployeePassword), UserRole.Employee);
        _context.Users.AddRange(admin, employee1, employee2);

        var milk = Product.Create("Leche Entera", "Alpina", "Lácteos", UnitType.Volume, 1m, 2500m, 3200m);
        var yogurt = Product.Create("Yogurt Fresa", "Alpina", "Lácteos", UnitType.Volume, 0.5m, 1800m, 2400m);
        var rice = Product.Create("Arroz Blanco", "Diana", "Granos", UnitType.Weight, 1m, 3200m, 4100m);
        var bread = Product.Create("Pan Tajado", "Bimbo", "Panadería", UnitType.Unit, 1m, 4500m, 6200m);
        var cola = Product.Create("Gaseosa Cola", "Postobón", "Bebidas", UnitType.Volume, 1.5m, 3000m, 3000m);
        var detergent = Product.Create("Detergente Líquido", "Fab", "Aseo", UnitType.Volume, 2m, 9000m, 12500m);
        var eggs = Product.Create("Huevos AA x30", "Kikes", "Proteínas", UnitType.Unit, 30m, 14000m, 17500m);
        var tuna = Product.Create("Atún en Agua", "Van Camp's", "Enlatados", UnitType.Weight, 0.16m, 4200m, 5600m);
        tuna.Deactivate();
        _context.Products.AddRange(milk, yogurt, rice, bread, cola, detergent, eggs, tuna);

        _context.WarehouseStocks.AddRange(
            Lot(milk, "L001", "A-01", 10m, 5m, now, -20, 12),
            Lot(milk, "L002", "A-01", 20m, 5m, now, -10, 42),
            Lot(milk, "L003", "A-02", 30m, 5m, now, -5, 72),
            Lot(yogurt, "YOG-001", "A-03", 8m, 3m, now, -40, -5),
            Lot(rice, "ARR-001", "B-01", 5m, 10m, now, -60, 300),
            Lot(bread, "PAN-001", "C-01", 40m, 10m, now, -2, 6),
            Lot(cola, "GAS-001", "B-02", 60m, 12m, now, -30, 150),
            Lot(eggs, "HUE-001", "C-02", 3m, 2m, now, -4, 20));

        var s1 = Sale.Create(employee1.Id, now.AddDays(-7));
        s1.AddItem(bread.Id, 2m, 5900m);
        s1.Complete();

        var s2 = Sale.Create(employee1.Id, now.AddDays(-3));
        s2.AddItem(milk.Id, 4m, 3200m);
        s2.AddItem(cola.Id, 1m, 3000m);
        s2.Complete();
        _context.Sales.AddRange(s1, s2);

        var reviewLots = UserTask.Create(employee1.Id, "Revisar lotes por vencer", "Revisar la bodega A", now.AddDays(2));
        var restockRice = UserTask.Create(employee1.Id, "Reponer arroz", "Solicitar reposición de arroz", now.AddDays(5));
        restockRice.Start();
        var countWarehouse = UserTask.Create(employee1.Id, "Conteo de bodega A", "Conteo mensual", now.AddDays(-1));
        countWarehouse.Start();
        countWarehouse.Complete();
        var labelDairy = UserTask.Create(employee2.Id, "Etiquetar lácteos", "Etiquetado de la nevera", now.AddDays(3));
        _context.Tasks.AddRange(reviewLots, restockRice, countWarehouse, labelDairy);

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static WarehouseStock Lot(
        Product product,
        string batch,
        string location,
        decimal quantity,
        decimal minimumStock,
        DateTime now,
        int receivedOffsetDays,
        int expirationOffsetDays) =>
        WarehouseStock.Create(
            product.Id,
            batch,
            location,
            quantity,
            minimumStock,
            now.AddDays(receivedOffsetDays),
            now.AddDays(expirationOffsetDays));
}
