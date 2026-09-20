using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Market.Tests.Infraestructure;

[TestClass]
public static class SqlServerFixture
{
    private static MsSqlContainer? _container;

    [AssemblyInitialize]
    public static async Task StartAsync(TestContext context)
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await _container.StartAsync();
    }

    [AssemblyCleanup]
    public static async Task StopAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }

    public static string NewDatabaseConnectionString()
    {
        var builder = new SqlConnectionStringBuilder(_container!.GetConnectionString())
        {
            InitialCatalog = $"Supermarket_{Guid.NewGuid():N}"
        };

        return builder.ConnectionString;
    }
}
