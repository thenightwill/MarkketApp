using Application;
using Application.Interfaces.Authentication;
using Domain.Enums;
using Infraestructure;
using Infraestructure.Persistance;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Market.Tests.Infraestructure;

public sealed class CurrentUserHolder : ICurrentUser
{
    public Guid UserId { get; set; } = Guid.NewGuid();

    public UserRole Role { get; set; } = UserRole.Employee;
}

public sealed class TestHost : IAsyncDisposable
{
    public const string JwtKey = "test-signing-key-with-at-least-32-bytes-of-length!";

    private TestHost(ServiceProvider services, CurrentUserHolder currentUser)
    {
        Services = services;
        CurrentUser = currentUser;
    }

    public ServiceProvider Services { get; }

    public CurrentUserHolder CurrentUser { get; }

    public static async Task<TestHost> CreateAsync(bool seed = false)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:SupermarketDb"] = SqlServerFixture.NewDatabaseConnectionString(),
                ["Jwt:Key"] = JwtKey,
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:ExpirationMinutes"] = "30",
                ["Seed:AdminPassword"] = "Admin123!",
                ["Seed:EmployeePassword"] = "Employee123!"
            })
            .Build();

        var currentUser = new CurrentUserHolder();
        var services = new ServiceCollection()
            .AddApplication()
            .AddInfraestructure(configuration)
            .AddSingleton<ICurrentUser>(currentUser)
            .BuildServiceProvider();

        await DatabaseInitializer.InitializeAsync(services, seed);

        return new TestHost(services, currentUser);
    }

    public AsyncServiceScope Scope() => Services.CreateAsyncScope();

    public ValueTask DisposeAsync() => Services.DisposeAsync();
}
