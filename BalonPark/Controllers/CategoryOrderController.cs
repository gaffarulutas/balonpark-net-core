using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Services;

namespace BalonPark.Controllers
{
    [ApiController]
    [Route("api/category-order")]
    public class CategoryOrderController(
        CategoryRepository categoryRepository,
        SubCategoryRepository subCategoryRepository,
        ICacheService cacheService,
        ILogger<CategoryOrderController> logger) : ControllerBase
    {

        /// <summary>
        /// Kategorileri yeniden sıralar
        /// </summary>
        [HttpPost("categories/reorder")]
        public async Task<IActionResult> ReorderCategories([FromBody] List<CategoryOrderDto> categories)
        {
            try
            {
                if (categories == null || !categories.Any())
                {
                    return BadRequest(new { success = false, message = "Kategori listesi boş olamaz" });
                }

                // Dictionary'ye dönüştür (categoryId -> displayOrder)
                var orderMap = categories.ToDictionary(c => c.Id, c => c.DisplayOrder);

                var success = await categoryRepository.ReorderCategoriesAsync(orderMap);

                if (success)
                {
                    // Cache'i temizle
                    await cacheService.InvalidateCategoriesAsync();
                    
                    logger.LogInformation("Kategoriler başarıyla yeniden sıralandı. Toplam: {Count}", categories.Count);
                    return Ok(new { success = true, message = $"{categories.Count} kategori başarıyla sıralandı" });
                }

                return BadRequest(new { success = false, message = "Sıralama işlemi başarısız" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kategori sıralama hatası");
                return StatusCode(500, new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        /// <summary>
        /// Alt kategorileri yeniden sıralar
        /// </summary>
        [HttpPost("subcategories/reorder")]
        public async Task<IActionResult> ReorderSubCategories([FromBody] List<CategoryOrderDto> subCategories)
        {
            try
            {
                if (subCategories == null || !subCategories.Any())
                {
                    return BadRequest(new { success = false, message = "Alt kategori listesi boş olamaz" });
                }

                // Dictionary'ye dönüştür (subCategoryId -> displayOrder)
                var orderMap = subCategories.ToDictionary(sc => sc.Id, sc => sc.DisplayOrder);

                var success = await subCategoryRepository.ReorderSubCategoriesAsync(orderMap);

                if (success)
                {
                    // Cache'i temizle
                    await cacheService.InvalidateSubCategoriesAsync();
                    
                    logger.LogInformation("Alt kategoriler başarıyla yeniden sıralandı. Toplam: {Count}", subCategories.Count);
                    return Ok(new { success = true, message = $"{subCategories.Count} alt kategori başarıyla sıralandı" });
                }

                return BadRequest(new { success = false, message = "Sıralama işlemi başarısız" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Alt kategori sıralama hatası");
                return StatusCode(500, new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        /// <summary>
        /// Tek bir kategorinin sırasını günceller
        /// </summary>
        [HttpPut("categories/{id}/order")]
        public async Task<IActionResult> UpdateCategoryOrder(int id, [FromBody] UpdateOrderDto dto)
        {
            try
            {
                var success = await categoryRepository.UpdateDisplayOrderAsync(id, dto.DisplayOrder);

                if (success)
                {
                    // Cache'i temizle
                    await cacheService.InvalidateCategoriesAsync();
                    
                    return Ok(new { success = true, message = "Kategori sırası güncellendi" });
                }

                return NotFound(new { success = false, message = "Kategori bulunamadı" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kategori sırası güncelleme hatası: {Id}", id);
                return StatusCode(500, new { success = false, message = $"Hata: {ex.Message}" });
            }
        }

        /// <summary>
        /// Tek bir alt kategorinin sırasını günceller
        /// </summary>
        [HttpPut("subcategories/{id}/order")]
        public async Task<IActionResult> UpdateSubCategoryOrder(int id, [FromBody] UpdateOrderDto dto)
        {
            try
            {
                var success = await subCategoryRepository.UpdateDisplayOrderAsync(id, dto.DisplayOrder);

                if (success)
                {
                    // Cache'i temizle
                    await cacheService.InvalidateSubCategoriesAsync();
                    
                    return Ok(new { success = true, message = "Alt kategori sırası güncellendi" });
                }

                return NotFound(new { success = false, message = "Alt kategori bulunamadı" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Alt kategori sırası güncelleme hatası: {Id}", id);
                return StatusCode(500, new { success = false, message = $"Hata: {ex.Message}" });
            }
        }
    }

    // DTO sınıfları
    public class CategoryOrderDto
    {
        public int Id { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class UpdateOrderDto
    {
        public int DisplayOrder { get; set; }
    }
}

