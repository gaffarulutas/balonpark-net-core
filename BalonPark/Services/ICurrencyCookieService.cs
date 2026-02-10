namespace BalonPark.Services;

public interface ICurrencyCookieService
{
    string GetSelectedCurrency();
    void SetSelectedCurrency(string currency);
    string GetDefaultCurrency();
}
