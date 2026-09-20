using Application.Authentication;
using Application.Inventory;
using Application.Products;
using Application.Sales;
using Application.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<RegisterUseCase>();
        services.AddScoped<LoginUseCase>();

        services.AddScoped<CreateProductUseCase>();
        services.AddScoped<UpdateProductUseCase>();
        services.AddScoped<DeactivateProductUseCase>();
        services.AddScoped<GetProductsUseCase>();
        services.AddScoped<GetProductByIdUseCase>();

        services.AddScoped<CreateStockUseCase>();
        services.AddScoped<UpdateStockUseCase>();
        services.AddScoped<AddStockQuantityUseCase>();
        services.AddScoped<GetStocksUseCase>();
        services.AddScoped<GetStockByIdUseCase>();

        services.AddScoped<CreateSaleUseCase>();
        services.AddScoped<GetSalesUseCase>();
        services.AddScoped<GetSaleByIdUseCase>();

        services.AddScoped<CreateUserTaskUseCase>();
        services.AddScoped<UpdateUserTaskUseCase>();
        services.AddScoped<GetUserTasksUseCase>();
        services.AddScoped<GetUserTaskByIdUseCase>();
        services.AddScoped<DeleteUserTaskUseCase>();

        return services;
    }
}
