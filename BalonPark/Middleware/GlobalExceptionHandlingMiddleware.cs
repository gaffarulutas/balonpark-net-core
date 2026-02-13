using Serilog;

namespace BalonPark.Middleware;

public class GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            // Detaylı hata logu
            logger.LogError(ex, 
                "Global Exception Handler - Beklenmeyen hata oluştu. " +
                "Path: {Path}, Method: {Method}, User: {User}, IP: {IP}, Query: {Query}", 
                context.Request.Path, 
                context.Request.Method,
                context.User?.Identity?.Name ?? "Anonim",
                context.Connection.RemoteIpAddress?.ToString() ?? "Bilinmiyor",
                context.Request.QueryString.ToString());

            // Serilog'a da yaz
            Log.Error(ex, 
                "FATAL ERROR - Path: {Path}, Method: {Method}, Query: {Query}", 
                context.Request.Path,
                context.Request.Method,
                context.Request.QueryString);

            // Hata sayfasına yönlendir (Redirect 302 döner; status code set etmeye gerek yok)
            if (!context.Response.HasStarted)
            {
                context.Response.Redirect("/Error");
            }
        }
    }
}

public static class GlobalExceptionHandlingMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionHandlingMiddleware>();
    }
}

