using BalonPark.Models;

namespace BalonPark.Services;

/// <summary>
/// Google Gemini/Imagen ile ürün görseli üretimi.
/// </summary>
public interface IGeminiImageService
{
    /// <summary>
    /// Ürün bilgilerine göre en az 6 adet gerçekçi, kare (1:1) ürün görseli üretir.
    /// Her görsel farklı açı/sahne ile oluşturulur.
    /// </summary>
    /// <param name="product">Başlık, özet ve açıklama kullanılır.</param>
    /// <param name="cancellationToken">İptal tokenı.</param>
    /// <returns>Base64 PNG/JPEG verileri (data URL değil, sadece base64 string).</returns>
    Task<IReadOnlyList<string>> GenerateProductImagesAsync(Product product, CancellationToken cancellationToken = default);
}
