namespace BalonPark.Middleware;

/// <summary>
/// www.balonpark.com isteklerini balonpark.com'a 301 redirect ile yonlendirir.
/// Google Merchant Center icin domain tutarliligi saglar: tum URL'ler tek bir canonical domain uzerinden sunulur.
/// Bu, "alan adi sorunu" ve "online magazada urun yok" hatalarini onlemeye yardimci olur.
/// </summary>
public class CanonicalDomainMiddleware(RequestDelegate next, IConfiguration configuration)
{
    private readonly string? _canonicalHost = GetCanonicalHost(configuration);

    public async Task InvokeAsync(HttpContext context)
    {
        if (_canonicalHost != null && context.Request.Host.Host != _canonicalHost)
        {
            var isWwwRedirect = context.Request.Host.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase);
            if (isWwwRedirect)
            {
                var newUrl = $"{context.Request.Scheme}://{_canonicalHost}{context.Request.PathBase}{context.Request.Path}{context.Request.QueryString}";
                context.Response.StatusCode = 301;
                context.Response.Headers.Location = newUrl;
                return;
            }
        }

        await next(context);
    }

    private static string? GetCanonicalHost(IConfiguration configuration)
    {
        var siteUrl = configuration["siteUrl"];
        if (string.IsNullOrEmpty(siteUrl))
            return null;

        if (Uri.TryCreate(siteUrl, UriKind.Absolute, out var uri))
            return uri.Host;

        return null;
    }
}

public static class CanonicalDomainMiddlewareExtensions
{
    public static IApplicationBuilder UseCanonicalDomain(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<CanonicalDomainMiddleware>();
    }
}
