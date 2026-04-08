using System.Net;
using System.Text.Json;

namespace LedgerCore.Helpers;

public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Business validation error.");
            await WriteProblemDetailsAsync(
                context,
                statusCode: (int)HttpStatusCode.BadRequest,
                title: "Business validation error",
                detail: ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Unauthorized access.");
            await WriteProblemDetailsAsync(
                context,
                statusCode: (int)HttpStatusCode.Forbidden,
                title: "Forbidden",
                detail: ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception.");
            await WriteProblemDetailsAsync(
                context,
                statusCode: (int)HttpStatusCode.InternalServerError,
                title: "Server error",
                detail: "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblemDetailsAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;

        var payload = new
        {
            type = $"https://httpstatuses.com/{statusCode}",
            title,
            status = statusCode,
            detail,
            traceId = context.TraceIdentifier
        };

        var json = JsonSerializer.Serialize(payload);
        await context.Response.WriteAsync(json);
    }
}