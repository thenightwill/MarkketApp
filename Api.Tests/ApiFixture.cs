using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace Market.Tests.Api;

public sealed class ApiFactory : WebApplicationFactory<WebAPI.Program>
{
    private readonly string _connectionString;

    public ApiFactory(string connectionString) => _connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:SupermarketDb", _connectionString);
        builder.UseSetting("Jwt:Key", ApiFixture.JwtKey);
        builder.UseSetting("Database:MigrateOnStartup", "true");
        builder.UseSetting("Seed:Enabled", "true");
        builder.UseSetting("Seed:AdminPassword", ApiFixture.AdminPassword);
        builder.UseSetting("Seed:EmployeePassword", ApiFixture.EmployeePassword);
    }
}

[TestClass]
public static class ApiFixture
{
    public const string JwtKey = "api-tests-signing-key-with-at-least-32-bytes!";
    public const string AdminPassword = "Admin123!";
    public const string EmployeePassword = "Employee123!";

    private static MsSqlContainer? _container;

    public static ApiFactory Factory { get; private set; } = null!;

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext context)
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await _container.StartAsync();

        Factory = new ApiFactory(_container.GetConnectionString());
        Factory.CreateClient().Dispose();
    }

    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        if (Factory is not null)
            await Factory.DisposeAsync();

        if (_container is not null)
            await _container.DisposeAsync();
    }
}
