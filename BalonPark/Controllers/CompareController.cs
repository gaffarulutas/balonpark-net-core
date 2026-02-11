using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;

namespace BalonPark.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CompareController(
        ProductRepository productRepository,
        ProductImageRepository productImageRepository) : ControllerBase
    {

        [HttpPost("products")]
        public async Task<IActionResult> GetCompareProducts([FromBody] CompareRequest request)
        {
            try
            {
                if (request?.ProductIds == null || !request.ProductIds.Any())
                {
                    return BadRequest(new { success = false, message = "Ürün ID'leri gerekli" });
                }

                var products = new List<CompareProductDto>();

                foreach (var productId in request.ProductIds)
                {
                    var product = await productRepository.GetByIdAsync(productId);
                    if (product == null) continue;

                    var mainImage = await productImageRepository.GetMainImageAsync(productId);

                    products.Add(new CompareProductDto
                    {
                        Id = product.Id,
                        Name = product.Name,
                        Description = product.Description,
                        TechnicalDescription = product.TechnicalDescription,
                        Summary = product.Summary,
                        Price = product.Price,
                        UsdPrice = product.UsdPrice,
                        EuroPrice = product.EuroPrice,
                        Stock = product.Stock,
                        ProductCode = $"U-{product.Id}",
                        CategoryName = product.CategoryName,
                        SubCategoryName = product.SubCategoryName,
                        ImageUrl = mainImage?.ThumbnailPath ?? "/assets/images/no-image.png",
                        Slug = product.Slug,
                        CategorySlug = product.CategorySlug,
                        SubCategorySlug = product.SubCategorySlug
                    });
                }

                return Ok(new { success = true, products });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
    }

    public class CompareRequest
    {
        public List<int> ProductIds { get; set; } = new();
    }

    public class CompareProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? TechnicalDescription { get; set; }
        public string? Summary { get; set; }
        public decimal Price { get; set; }
        public decimal UsdPrice { get; set; }
        public decimal EuroPrice { get; set; }
        public int Stock { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? SubCategoryName { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? CategorySlug { get; set; }
        public string? SubCategorySlug { get; set; }
    }
}

