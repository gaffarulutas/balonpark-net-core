using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Services;

namespace BalonPark.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController(
        ProductRepository productRepository,
        ProductImageRepository productImageRepository,
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        IUrlService urlService) : ControllerBase
    {

        [HttpGet("products")]
        public async Task<IActionResult> SearchProducts([FromQuery] string q, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new { results = new List<object>() });

            // URL decode for Turkish characters
            var decodedQuery = System.Web.HttpUtility.UrlDecode(q, System.Text.Encoding.UTF8);
            
            var products = await productRepository.SearchAsync(decodedQuery, limit);
            
            var results = products.Select(p => new
            {
                title = p.Name,
                url = $"/category/{p.CategorySlug}/{p.SubCategorySlug}/{p.Slug}",
                image = urlService.GetImageUrl("/assets/images/no-image.png"), // Default image
                price = $"₺{p.Price:N2}",
                category = p.CategoryName,
                subcategory = p.SubCategoryName
            }).ToList();

            return Ok(new { results });
        }

        [HttpGet("categories")]
        public async Task<IActionResult> SearchCategories([FromQuery] string q, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new { results = new List<object>() });

            // URL decode for Turkish characters
            var decodedQuery = System.Web.HttpUtility.UrlDecode(q, System.Text.Encoding.UTF8);
            
            var categories = await categoryRepository.SearchAsync(decodedQuery, limit);
            
            var results = categories.Select(c => new
            {
                title = c.Name,
                url = $"/category/{c.Slug}",
                image = "/assets/images/logo/logo.png"
            }).ToList();

            return Ok(new { results });
        }

        [HttpGet("subcategories")]
        public async Task<IActionResult> SearchSubCategories([FromQuery] string q, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new { results = new List<object>() });

            // URL decode for Turkish characters
            var decodedQuery = System.Web.HttpUtility.UrlDecode(q, System.Text.Encoding.UTF8);
            
            var subCategories = await subCategoryRepository.SearchAsync(decodedQuery, limit);
            
            var results = subCategories.Select(sc => new
            {
                title = sc.Name,
                url = $"/category/{sc.CategorySlug}/{sc.Slug}",
                image = urlService.GetImageUrl("/assets/images/logo/logo.png"),
                category = sc.CategoryName
            }).ToList();

            return Ok(new { results });
        }

        [HttpGet("all")]
        public async Task<IActionResult> SearchAll([FromQuery] string q, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Ok(new { results = new List<object>() });

            // URL decode for Turkish characters
            var decodedQuery = System.Web.HttpUtility.UrlDecode(q, System.Text.Encoding.UTF8);
            
            // Her kategori için limit/3 kadar sonuç al (toplam limit'i geçmemek için)
            var categoryLimit = Math.Max(1, limit / 3);
            
            var productTask = productRepository.SearchAsync(decodedQuery, categoryLimit);
            var categoryTask = categoryRepository.SearchAsync(decodedQuery, categoryLimit);
            var subCategoryTask = subCategoryRepository.SearchAsync(decodedQuery, categoryLimit);

            await Task.WhenAll(productTask, categoryTask, subCategoryTask);

            var products = await productTask;
            var categories = await categoryTask;
            var subCategories = await subCategoryTask;

            var results = new List<object>();

            // Products - gerçek resimlerle
            foreach (var product in products)
            {
                // Her ürün için ana resmi al
                var mainImage = await productImageRepository.GetMainImageAsync(product.Id);
                var imageUrl = mainImage != null 
                    ? urlService.GetImageUrl(mainImage.ThumbnailPath)
                    : urlService.GetImageUrl("/assets/images/no-image.png");
                
                results.Add(new
                {
                    title = product.Name,
                    url = $"/category/{product.CategorySlug}/{product.SubCategorySlug}/{product.Slug}",
                    image = imageUrl,
                    price = $"₺{product.Price:N2}",
                    category = product.CategoryName,
                    subcategory = product.SubCategoryName,
                    type = "product"
                });
            }

            // Categories
            results.AddRange(categories.Select(c => new
            {
                title = c.Name,
                url = $"/category/{c.Slug}",
                image = urlService.GetImageUrl("/assets/images/logo/logo.png"),
                type = "category"
            }));

            // SubCategories
            results.AddRange(subCategories.Select(sc => new
            {
                title = sc.Name,
                url = $"/category/{sc.CategorySlug}/{sc.Slug}",
                image = urlService.GetImageUrl("/assets/images/logo/logo.png"),
                category = sc.CategoryName,
                type = "subcategory"
            }));

            // Toplam sonuç sayısını limit ile sınırla
            var limitedResults = results.Take(limit).ToList();

            return Ok(new { results = limitedResults });
        }


    }
}
