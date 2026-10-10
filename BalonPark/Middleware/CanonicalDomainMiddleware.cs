using BalonPark.Services;

namespace BalonPark.Middleware;

/// <summary>
/// balonpark.com dışındaki hostları (www, ipv4 ve benzeri) https://balonpark.com adresine 301 ile yönlendirir.
/// </summary>
public class CanonicalDomainMiddleware(RequestDelegate next, IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (!environment.IsDevelopment()
            && !host.Equals("balonpark.com", StringComparison.OrdinalIgnoreCase))
        {
            var newUrl = $"{UrlService.CanonicalBaseUrl}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
            context.Response.StatusCode = StatusCodes.Status301MovedPermanently;
            context.Response.Headers.Location = newUrl;
            return;
        }

        await next(context);
    }
}

public static class CanonicalDomainMiddlewareExtensions
{
    public static IApplicationBuilder UseCanonicalDomain(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CanonicalDomainMiddleware>();
    }
}
