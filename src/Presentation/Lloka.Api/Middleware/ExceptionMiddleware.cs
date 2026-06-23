using System.Text.Json;
using Lloka.Application.Common.Exceptions;
using Lloka.Domain.Common;

namespace Lloka.Api.Middleware;

public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await WriteErrorAsync(context, ex);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, Exception ex)
    {
        context.Response.ContentType = "application/json";

        int    status;
        string json;

        switch (ex)
        {
            case NotFoundException e:
                status = 404;
                json   = JsonSerializer.Serialize(new { error = e.Message }, JsonOptions);
                break;
            case ConflictException e:
                status = 409;
                json   = JsonSerializer.Serialize(new { error = e.Message }, JsonOptions);
                break;
            case UnauthorizedException e:
                status = 401;
                json   = JsonSerializer.Serialize(new { error = e.Message }, JsonOptions);
                break;
            case ValidationException e:
                status = 400;
                json   = JsonSerializer.Serialize(new { errors = e.Errors }, JsonOptions);
                break;
            case DomainException e:
                status = 422;
                json   = JsonSerializer.Serialize(new { error = e.Message }, JsonOptions);
                break;
            default:
                status = 500;
                json   = JsonSerializer.Serialize(new { error = "Ocurrió un error inesperado." }, JsonOptions);
                break;
        }

        context.Response.StatusCode = status;
        return context.Response.WriteAsync(json);
    }
}
