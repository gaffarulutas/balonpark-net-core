using Microsoft.AspNetCore.Mvc;
using BalonPark.Services;

namespace BalonPark.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CurrencyController(
    CurrencyService currencyService,
    ICurrencyCookieService currencyCookieService,
    IYandexExchangeRateService yandexExchangeRateService) : ControllerBase
{

    [HttpGet]
    public async Task<IActionResult> GetCurrencies()
    {
        try
        {
            var currencies = await currencyService.GetCurrenciesAsync();
            var list = currencies.Currencies.Select(c => new
            {
                code = c.Code,
                name = c.Name,
                rate = c.Rate,
                lastUpdated = c.LastUpdated
            }).ToList();

            var tryToRub = await yandexExchangeRateService.GetTryToRubRateAsync();
            if (tryToRub > 0)
            {
                list.Add(new
                {
                    code = "RUB",
                    name = "Rus Rublesi",
                    rate = 1m / tryToRub,
                    lastUpdated = DateTime.Now
                });
            }

            var response = new
            {
                currencies = list,
                lastUpdated = currencies.LastUpdated,
                success = true
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Döviz kurları alınırken hata oluştu",
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("usd")]
    public async Task<IActionResult> GetUsdRate()
    {
        try
        {
            var rate = await currencyService.GetUsdRateAsync();
            return Ok(new { rate, currency = "USD", success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "USD kuru alınırken hata oluştu",
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("eur")]
    public async Task<IActionResult> GetEurRate()
    {
        try
        {
            var rate = await currencyService.GetEurRateAsync();
            return Ok(new { rate, currency = "EUR", success = true });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "EUR kuru alınırken hata oluştu",
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpPost("set")]
    public IActionResult SetCurrency([FromBody] SetCurrencyRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Currency))
            {
                return BadRequest(new
                {
                    message = "Currency parametresi gerekli",
                    success = false
                });
            }

            currencyCookieService.SetSelectedCurrency(request.Currency);

            return Ok(new
            {
                message = "Para birimi başarıyla güncellendi",
                currency = request.Currency,
                success = true
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Para birimi güncellenirken hata oluştu",
                success = false,
                error = ex.Message
            });
        }
    }

    [HttpGet("current")]
    public IActionResult GetCurrentCurrency()
    {
        try
        {
            var currentCurrency = currencyCookieService.GetSelectedCurrency();
            return Ok(new
            {
                currency = currentCurrency,
                success = true
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "Mevcut para birimi alınırken hata oluştu",
                success = false,
                error = ex.Message
            });
        }
    }
}

public class SetCurrencyRequest
{
    public string Currency { get; set; } = string.Empty;
}
