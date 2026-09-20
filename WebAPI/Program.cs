using Infraestructure.Persistance;
using WebAPI.Extensions;

namespace WebAPI;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddApiServices(builder.Configuration);

        var app = builder.Build();

        if (app.Configuration.GetValue<bool>("Database:MigrateOnStartup"))
        {
            await DatabaseInitializer.InitializeAsync(
                app.Services,
                seed: app.Configuration.GetValue<bool>("Seed:Enabled"));
        }

        app.UseApiPipeline();

        await app.RunAsync();
    }
}
