using System.Text.Json.Serialization;
using Application;
using Application.Common;
using Application.Interfaces.Authentication;
using Infraestructure;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;
using WebAPI.Middleware;
using WebAPI.OpenApi;
using WebAPI.Security;

namespace WebAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public const string CorsPolicy = "Web";

    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfraestructure(configuration);
        services.AddJwtAuthentication(configuration);

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        services
            .AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .ToDictionary(
                            e => e.Key,
                            e => e.Value!.Errors.Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Invalid value." : x.ErrorMessage).ToArray());

                    var problem = new ValidationProblemDetails(errors)
                    {
                        Status = StatusCodes.Status400BadRequest,
                        Title = "Bad Request",
                        Detail = "One or more validation errors occurred."
                    };
                    problem.Extensions["code"] = AppErrorCodes.ValidationError;

                    return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
                };
            });

        var origins = configuration.GetSection("Cors:Origins").Get<string[]>()
            ?? new[] { "http://localhost:4200", "http://127.0.0.1:4200" };
        services.AddCors(options => options.AddPolicy(CorsPolicy, policy =>
            policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<ApiInfoDocumentTransformer>();
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<AuthorizeOperationTransformer>();
        });

        return services;
    }

    public static IApplicationBuilder UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options => options.WithTitle("Supermarket API"));
        }

        app.UseHttpsRedirection();
        app.UseCors(CorsPolicy);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
