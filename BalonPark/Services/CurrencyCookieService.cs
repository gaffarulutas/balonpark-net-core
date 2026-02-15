using Microsoft.AspNetCore.Http;

namespace BalonPark.Services;

public class CurrencyCookieService(IHttpContextAccessor httpContextAccessor) : ICurrencyCookieService
{
    private const string CURRENCY_COOKIE_NAME = "selected_currency";
    private const string DEFAULT_CURRENCY = "TL";

    public string GetSelectedCurrency()
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.Request.Cookies.ContainsKey(CURRENCY_COOKIE_NAME) == true)
        {
            var cookieValue = httpContext.Request.Cookies[CURRENCY_COOKIE_NAME];
            if (!string.IsNullOrEmpty(cookieValue) && IsValidCurrency(cookieValue))
            {
                return cookieValue;
            }
        }

        // Cookie yoksa veya geçersizse default currency'yi set et
        SetSelectedCurrency(DEFAULT_CURRENCY);
        return DEFAULT_CURRENCY;
    }

    public void SetSelectedCurrency(string currency)
    {
        if (!IsValidCurrency(currency))
        {
            currency = DEFAULT_CURRENCY;
        }

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = false, // JavaScript'ten erişilebilir olması için
                Secure = false, // HTTPS gerektirmez
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.Now.AddYears(1) // 1 yıl geçerli
            };

            httpContext.Response.Cookies.Append(CURRENCY_COOKIE_NAME, currency, cookieOptions);
        }
    }

    public string GetDefaultCurrency()
    {
        return DEFAULT_CURRENCY;
    }

    private static bool IsValidCurrency(string currency)
    {
        return currency == "TL" || currency == "USD" || currency == "EUR" || currency == "RUB";
    }
}
