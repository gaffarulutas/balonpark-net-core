namespace BalonPark.Middleware;

/// <summary>
/// Adds security-related HTTP response headers.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        if (context.Response.HasStarted)
            return;

        var headers = context.Response.Headers;

        // Clickjacking: prevent embedding in iframe
        if (!headers.ContainsKey("X-Frame-Options"))
            headers["X-Frame-Options"] = "SAMEORIGIN";

        // MIME sniffing: force declared content type
        if (!headers.ContainsKey("X-Content-Type-Options"))
            headers["X-Content-Type-Options"] = "nosniff";

        // XSS: legacy browsers (Content-Type already helps)
        if (!headers.ContainsKey("X-XSS-Protection"))
            headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer: send origin + path, not full URL to other origins
        if (!headers.ContainsKey("Referrer-Policy"))
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions: disable unused browser features
        if (!headers.ContainsKey("Permissions-Policy"))
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    }
}

public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
