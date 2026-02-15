using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages
{
    public class SearchModel : BasePage
    {
        private readonly ProductRepository _productRepository;
        private readonly ProductImageRepository _productImageRepository;
        private readonly CurrencyService _currencyService;
        private readonly IYandexExchangeRateService _yandexExchangeRateService;

        public SearchModel(ProductRepository productRepository, ProductImageRepository productImageRepository, CurrencyService currencyService, IYandexExchangeRateService yandexExchangeRateService, CategoryRepository categoryRepository, SubCategoryRepository subCategoryRepository, SettingsRepository settingsRepository, IUrlService urlService, ICurrencyCookieService currencyCookieService) : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _currencyService = currencyService;
        _yandexExchangeRateService = yandexExchangeRateService;
    }

        [BindProperty(SupportsGet = true)]
        public string Query { get; set; } = string.Empty;

        public IEnumerable<ProductWithImage> Products { get; set; } = new List<ProductWithImage>();

        public async Task OnGetAsync()
        {
            // Query parametresini Request'ten al
            var queryParam = Request.Query["q"].FirstOrDefault();
            if (!string.IsNullOrEmpty(queryParam))
            {
                Query = queryParam.Trim();
            }

            if (!string.IsNullOrEmpty(Query) && Query.Length >= 2)
            {
                var searchResults = (await _productRepository.SearchAsync(Query, 10)).ToList();
                var tryToRub = await _yandexExchangeRateService.GetTryToRubRateAsync();
                Products = new List<ProductWithImage>();
                foreach (var product in searchResults)
                {
                    var (usdPrice, euroPrice) = await _currencyService.CalculatePricesAsync(product.Price);
                    product.UsdPrice = Math.Round(usdPrice, 2);
                    product.EuroPrice = Math.Round(euroPrice, 2);
                    product.RubPrice = Math.Round(product.Price * tryToRub, 2);
                    var mainImage = await _productImageRepository.GetMainImageAsync(product.Id);
                    Products = Products.Append(new ProductWithImage
                    {
                        Product = product,
                        MainImage = mainImage
                    });
                }
            }
        }
    }

}
