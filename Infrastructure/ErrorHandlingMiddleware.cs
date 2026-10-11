namespace VeltrexAPI.Infrastructure;

/// <summary>Returns every error as JSON <c>{ "message": ... }</c>, which is what the frontend reads.</summary>
public class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ApiException ex)
        {
            await Write(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error on {Method} {Path}", context.Request.Method, context.Request.Path);
            await Write(context, StatusCodes.Status500InternalServerError, "Something went wrong. Please try again.");
        }
    }

    private static async Task Write(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted) return;
        context.Response.Clear();
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { message });
    }
}
