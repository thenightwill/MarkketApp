using Infraestructure.Persistance.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Infraestructure.Persistance;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, bool seed, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();

        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await context.Database.MigrateAsync(cancellationToken);

        if (seed)
            await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(cancellationToken);
    }
}
