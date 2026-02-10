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

    public SearchModel(ProductRepository productRepository, ProductImageRepository productImageRepository, CategoryRepository categoryRepository, SubCategoryRepository subCategoryRepository, SettingsRepository settingsRepository, IUrlService urlService, ICurrencyCookieService currencyCookieService) : base(categoryRepository, subCategoryRepository, settingsRepository, urlService, currencyCookieService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
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
                var searchResults = await _productRepository.SearchAsync(Query, 10);
                
                Products = new List<ProductWithImage>();
                
                foreach (var product in searchResults)
                {
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
