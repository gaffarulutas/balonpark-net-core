using Microsoft.AspNetCore.Mvc;
using BalonPark.Attributes;
using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Controllers;

/// <summary>
/// Admin: AI ile üretilen ürün görsellerini kaydetme. JSON POST, antiforgery yok (admin session gerekli).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[RequireAdminSession]
[IgnoreAntiforgeryToken]
public class ProductImageAdminController : ControllerBase
{
    private readonly ProductRepository _productRepository;
    private readonly ProductImageRepository _productImageRepository;
    private readonly IWebHostEnvironment _environment;
    private readonly ICacheService _cacheService;

    public ProductImageAdminController(
        ProductRepository productRepository,
        ProductImageRepository productImageRepository,
        IWebHostEnvironment environment,
        ICacheService cacheService)
    {
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _environment = environment;
        _cacheService = cacheService;
    }

    /// <summary>
    /// Modal'dan seçilen AI üretim görsellerini ürüne ekler. Body: { productId: number, images: string[] } (base64 veya data URL).
    /// </summary>
    [HttpPost("SaveGeneratedImages")]
    public async Task<IActionResult> SaveGeneratedImages([FromBody] SaveGeneratedImagesRequest request)
    {
        if (request?.ProductId <= 0 || request?.Images == null || !request.Images.Any())
            return BadRequest(new { success = false, message = "Geçersiz istek." });

        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
            return NotFound(new { success = false, message = "Ürün bulunamadı." });

        var uploadPath = Path.Combine(_environment.WebRootPath, "uploads", "products", product.Id.ToString());
        Directory.CreateDirectory(uploadPath);
        var watermarkLogoPath = Path.Combine(_environment.WebRootPath, "assets", "images", "logo", "logo.png");
        var existingImages = (await _productImageRepository.GetByProductIdAsync(product.Id)).ToList();
        var displayOrder = existingImages.Any() ? existingImages.Max(x => x.DisplayOrder) + 1 : 1;
        var hasMainImage = existingImages.Any(x => x.IsMainImage);
        var saved = 0;

        foreach (var dataUrl in request.Images)
        {
            if (string.IsNullOrWhiteSpace(dataUrl)) continue;
            var base64 = dataUrl.Contains(",", StringComparison.Ordinal) ? dataUrl.Split(',', 2)[1].Trim() : dataUrl;
            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64);
            }
            catch
            {
                continue;
            }
            if (bytes.Length == 0) continue;

            var fileName = $"{Guid.NewGuid()}.png";
            var webpFileName = ImageHelper.ToWebPFileName(fileName);
            await ImageHelper.SaveProductImageFromBytesAsync(
                bytes,
                uploadPath,
                fileName,
                System.IO.File.Exists(watermarkLogoPath) ? "/assets/images/logo/logo.png" : null,
                watermarkOpacity: 0.3f,
                watermarkScale: 0.1f
            );

            var productImage = new ProductImage
            {
                ProductId = product.Id,
                FileName = webpFileName,
                OriginalPath = $"uploads/products/{product.Id}/original_{fileName}",
                LargePath = $"uploads/products/{product.Id}/large_{webpFileName}",
                ThumbnailPath = $"uploads/products/{product.Id}/thumb_{webpFileName}",
                IsMainImage = !hasMainImage,
                DisplayOrder = displayOrder,
                CreatedAt = DateTime.Now
            };
            await _productImageRepository.CreateAsync(productImage);
            if (!hasMainImage) hasMainImage = true;
            displayOrder++;
            saved++;
        }

        await _cacheService.InvalidateProductsAsync();
        await _cacheService.InvalidateProductAsync(product.Id);
        await _cacheService.InvalidateProductBySlugAsync(product.Slug);

        return Ok(new { success = true, message = $"{saved} resim ürüne eklendi.", count = saved });
    }
}

public class SaveGeneratedImagesRequest
{
    public int ProductId { get; set; }
    public List<string> Images { get; set; } = new();
}
