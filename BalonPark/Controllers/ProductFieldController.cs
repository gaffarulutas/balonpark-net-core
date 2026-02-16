using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using BalonPark.Attributes;
using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models;

namespace BalonPark.Controllers;

/// <summary>
/// Public ürün detay sayfasında admin için tek alan güncelleme API'si.
/// Sadece admin oturumu açıkken kullanılır.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ProductFieldController : ControllerBase
{
    private readonly ProductRepository _productRepository;

    /// <summary>
    /// İzin verilen alan adları (over-posting güvenliği).
    /// </summary>
    /// <summary>
    /// Admin panelindeki Ürün Düzenle ile aynı alanlar (CategoryId/SubCategoryId hariç; URL değişir).
    /// </summary>
    private static readonly HashSet<string> AllowedFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "Name", "Price", "Stock", "Summary", "Description", "TechnicalDescription",
        "InflatedLength", "InflatedWidth", "InflatedHeight", "UserCount", "InflatedWeightKg",
        "AssemblyTime", "RequiredPersonCount", "FanDescription", "FanWeightKg",
        "PackagedLength", "PackagedDepth", "PackagedWeightKg", "PackagePalletCount",
        "HasCertificate", "WarrantyDescription", "AfterSalesService",
        "IsDiscounted", "IsPopular", "IsProjectSpecial",
        "DeliveryDays", "DeliveryDaysMin", "DeliveryDaysMax",
        "IsFireResistant", "MaterialWeight", "MaterialWeightGrm2", "ColorOptions",
        "IsActive", "DisplayOrder"
    };

    public ProductFieldController(ProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    /// <summary>
    /// Ürünün tek bir alanını günceller. Admin oturumu gerekir.
    /// </summary>
    [HttpPost("update")]
    [RequireAdminSession]
    public async Task<IActionResult> UpdateField([FromBody] ProductFieldUpdateRequest request)
    {
        if (request == null || request.ProductId <= 0)
        {
            return BadRequest(new { success = false, message = "Geçersiz istek." });
        }

        if (string.IsNullOrWhiteSpace(request.Field) || !AllowedFields.Contains(request.Field))
        {
            return BadRequest(new { success = false, message = "İzin verilmeyen veya geçersiz alan." });
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product == null)
        {
            return NotFound(new { success = false, message = "Ürün bulunamadı." });
        }

        try
        {
            SetProductField(product, request.Field, request.Value);
            product.UpdatedAt = DateTime.Now;

            if (string.Equals(request.Field, "Name", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(product.Name))
            {
                product.Slug = SlugHelper.GenerateSlug(product.Name);
            }

            await _productRepository.UpdateAsync(product);

            // Slug değiştiyse (örn. Name güncellendi) yeni URL'ye yönlendirme bilgisi dön
            string? redirectPath = null;
            if (string.Equals(request.Field, "Name", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrEmpty(product.CategorySlug) && !string.IsNullOrEmpty(product.SubCategorySlug) && !string.IsNullOrEmpty(product.Slug))
            {
                redirectPath = $"/category/{product.CategorySlug}/{product.SubCategorySlug}/{product.Slug}";
            }

            return Ok(new
            {
                success = true,
                message = "Alan güncellendi.",
                value = GetFieldValueForResponse(product, request.Field),
                redirectPath
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "Güncelleme sırasında hata oluştu.", error = ex.Message });
        }
    }

    private static void SetProductField(Product product, string field, object? value)
    {
        if (value is JsonElement je)
        {
            value = je.ValueKind switch
            {
                JsonValueKind.String => je.GetString(),
                JsonValueKind.Number => je.TryGetInt32(out var i) ? i : je.GetDecimal(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => je.GetRawText()
            };
        }

        var prop = typeof(Product).GetProperty(field);
        if (prop == null || !prop.CanWrite)
            throw new ArgumentException("Alan yazılamıyor.");

        if (value == null)
        {
            if (prop.PropertyType == typeof(string) || Nullable.GetUnderlyingType(prop.PropertyType) != null)
            {
                prop.SetValue(product, null);
                return;
            }
            throw new ArgumentException("Bu alan boş bırakılamaz.");
        }

        if (prop.PropertyType == typeof(string))
        {
            prop.SetValue(product, value.ToString() ?? string.Empty);
            return;
        }

        if (prop.PropertyType == typeof(int))
        {
            if (value is int i) { prop.SetValue(product, i); return; }
            if (value is long l) { prop.SetValue(product, (int)l); return; }
            if (int.TryParse(value.ToString(), out var parsed)) { prop.SetValue(product, parsed); return; }
            throw new ArgumentException("Geçerli bir tam sayı girin.");
        }

        if (prop.PropertyType == typeof(decimal))
        {
            if (value is decimal d) { prop.SetValue(product, d); return; }
            if (value is double db) { prop.SetValue(product, (decimal)db); return; }
            if (value is int iv) { prop.SetValue(product, (decimal)iv); return; }
            if (decimal.TryParse(value.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                prop.SetValue(product, parsed);
                return;
            }
            throw new ArgumentException("Geçerli bir sayı girin.");
        }

        if (prop.PropertyType == typeof(bool))
        {
            if (value is bool b) { prop.SetValue(product, b); return; }
            if (value is string s && bool.TryParse(s, out var bp)) { prop.SetValue(product, bp); return; }
            if (value?.ToString()?.Trim() is "1" or "true" or "evet") { prop.SetValue(product, true); return; }
            if (value?.ToString()?.Trim() is "0" or "false" or "hayır") { prop.SetValue(product, false); return; }
            throw new ArgumentException("Geçerli bir değer girin (evet/hayır).");
        }

        var underlying = Nullable.GetUnderlyingType(prop.PropertyType);
        if (underlying == typeof(int))
        {
            if (value.ToString()?.Trim() is "" or null) { prop.SetValue(product, null); return; }
            if (int.TryParse(value.ToString(), out var parsed)) { prop.SetValue(product, parsed); return; }
            throw new ArgumentException("Geçerli bir tam sayı girin.");
        }

        if (underlying == typeof(decimal))
        {
            if (value.ToString()?.Trim() is "" or null) { prop.SetValue(product, null); return; }
            if (decimal.TryParse(value.ToString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                prop.SetValue(product, parsed);
                return;
            }
            throw new ArgumentException("Geçerli bir sayı girin.");
        }

        prop.SetValue(product, Convert.ChangeType(value, prop.PropertyType));
    }

    private static object? GetFieldValueForResponse(Product product, string field)
    {
        var prop = typeof(Product).GetProperty(field);
        return prop?.GetValue(product);
    }
}

public class ProductFieldUpdateRequest
{
    public int ProductId { get; set; }
    public string Field { get; set; } = string.Empty;
    public object? Value { get; set; }
}
