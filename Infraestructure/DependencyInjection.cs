using Application.Interfaces.Authentication;
using Application.Interfaces.Persistance;
using Infraestructure.Authentication;
using Infraestructure.Persistance;
using Infraestructure.Persistance.Repositories;
using Infraestructure.Persistance.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Infraestructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfraestructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("SupermarketDb")
            ?? throw new InvalidOperationException("The connection string 'SupermarketDb' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IWarehouseStockRepository, WarehouseStockRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<IUserTaskRepository, UserTaskRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.TryAddSingleton(TimeProvider.System);
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        services.AddScoped(sp =>
        {
            var options = new SeedOptions();
            configuration.GetSection(SeedOptions.SectionName).Bind(options);
            return options;
        });
        services.AddScoped<DataSeeder>();

        return services;
    }
}
