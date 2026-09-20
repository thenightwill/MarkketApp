using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WebAPI.OpenApi;

public sealed class ApiInfoDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Info = new()
        {
            Title = "Supermarket API",
            Version = "v1",
            Description =
                "API para la gestión de un supermercado: productos, inventario por lotes con FEFO, ventas y tareas. "
                + "Clean Architecture con Domain, Application, Infraestructure y WebAPI. "
                + "Todas las rutas, excepto /api/auth, requieren un token JWT Bearer obtenido en /api/auth/login o /api/auth/register."
        };

        return Task.CompletedTask;
    }
}
