using Application.Common;
using Application.Interfaces.Persistance;
using Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace WebAPI.Middleware;

public class ExceptionHandlingMiddleware
{
    private static readonly HashSet<string> ConflictDomainCodes = new()
    {
        DomainErrorCodes.InsufficientStock,
        DomainErrorCodes.InventoryExpired,
        DomainErrorCodes.InvalidTaskTransition,
        DomainErrorCodes.SaleAlreadyCompleted,
        DomainErrorCodes.DuplicateSaleItem
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            var status = ConflictDomainCodes.Contains(ex.Code) ? StatusCodes.Status409Conflict : StatusCodes.Status400BadRequest;
            await WriteAsync(context, status, ex.Code, ex.Message);
        }
        catch (AppException ex)
        {
            await WriteAsync(context, ToStatus(ex.Kind), ex.Code, ex.Message);
        }
        catch (ConcurrencyConflictException)
        {
            await WriteAsync(
                context,
                StatusCodes.Status409Conflict,
                AppErrorCodes.ConcurrencyConflict,
                "The data was modified by another request. Reload and try again.");
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteAsync(context, StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.");
        }
    }

    public static Task WriteAsync(HttpContext context, int status, string code, string detail)
    {
        if (context.Response.HasStarted)
            return Task.CompletedTask;

        context.Response.Clear();
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = ReasonPhrase(status),
            Detail = detail
        };
        problem.Extensions["code"] = code;

        return context.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json");
    }

    private static int ToStatus(AppErrorKind kind) => kind switch
    {
        AppErrorKind.Validation => StatusCodes.Status400BadRequest,
        AppErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
        AppErrorKind.Forbidden => StatusCodes.Status403Forbidden,
        AppErrorKind.NotFound => StatusCodes.Status404NotFound,
        AppErrorKind.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string ReasonPhrase(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "Bad Request",
        StatusCodes.Status401Unauthorized => "Unauthorized",
        StatusCodes.Status403Forbidden => "Forbidden",
        StatusCodes.Status404NotFound => "Not Found",
        StatusCodes.Status409Conflict => "Conflict",
        _ => "Internal Server Error"
    };
}
