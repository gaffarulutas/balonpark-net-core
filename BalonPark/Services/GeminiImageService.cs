using System.Text;
using System.Text.Json;
using BalonPark.Data;
using BalonPark.Models;

namespace BalonPark.Services;

/// <summary>
/// Google Gemini API ile ürün görseli üretimi (2025/2026: Gemini 2.5 Flash Image / Imagen).
/// Çocukların oynadığı şişme parklar için gerçekçi, kare (1:1), farklı açılardan en az 6 görsel üretir.
/// Öncelik: gemini-2.5-flash-image (generateContent), isteğe bağlı Imagen (predict, faturalandırma gerekir).
/// </summary>
public class GeminiImageService : IGeminiImageService
{
    private const string BaseUrl = "https://generativelanguage.googleapis.com/v1beta/models";
    /// <summary>2025/2026 önerilen model: API key ile generateContent, pay-as-you-go (Nano Banana).</summary>
    private const string GeminiImageModel = "gemini-2.5-flash-image";
    /// <summary>Imagen: predict endpoint, faturalandırma gerekir.</summary>
    private const string ImagenModel = "imagen-4.0-generate-001";
    private readonly HttpClient _httpClient;
    private readonly ILogger<GeminiImageService> _logger;
    private readonly SettingsRepository _settingsRepository;

    public GeminiImageService(
        HttpClient httpClient,
        SettingsRepository settingsRepository,
        ILogger<GeminiImageService> logger)
    {
        _httpClient = httpClient;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> GenerateProductImagesAsync(Product product, CancellationToken cancellationToken = default)
    {
        var settings = await _settingsRepository.GetFirstAsync();
        var apiKey = settings?.GeminiApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("Gemini API anahtarı tanımlı değil. Admin → Genel Ayarlar → Yapay Zeka API Anahtarları bölümünden Google Gemini API anahtarını girin.");
        var useImagenFallback = settings?.GeminiUseImagenFallback ?? false;

        var productContext = BuildProductContext(product);
        var prompts = BuildPrompts(productContext);

        var results = new List<string>();
        for (var i = 0; i < prompts.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                var base64 = await GenerateOneImageViaGeminiAsync(prompts[i], apiKey, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(base64))
                    results.Add(base64);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gemini image request {Index} failed: {Message}", i + 1, ex.Message);
                if (useImagenFallback)
                {
                    try
                    {
                        var fallback = await GenerateOneImageViaImagenAsync(prompts[i], apiKey, cancellationToken).ConfigureAwait(false);
                        if (!string.IsNullOrEmpty(fallback))
                            results.Add(fallback);
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogWarning(ex2, "Imagen fallback {Index} failed", i + 1);
                    }
                }
            }
        }

        return results;
    }

    private static string BuildProductContext(Product product)
    {
        var name = product.Name ?? "inflatable play park";
        var summary = product.Summary ?? "";
        var desc = product.Description ?? "";
        if (desc.Length > 400)
            desc = desc[..400] + "...";
        return $"Product: {name}. Summary: {summary}. Description: {desc}".Trim();
    }

    /// <summary>
    /// Her biri farklı açı/sahne olan en az 6 profesyonel prompt (İngilizce, Imagen için optimize).
    /// </summary>
    private static List<string> BuildPrompts(string productContext)
    {
        const string prefix = "Professional product photography, square 1:1 aspect ratio, realistic, high quality, commercial photo. Inflatable children's play park, safe and colorful. ";
        const string suffix = " Bright daylight, sharp focus, no text or logos. Photorealistic style.";
        return
        [
            prefix + productContext + " Front view, full structure visible, centered." + suffix,
            prefix + productContext + " Side view, three-quarter angle, showing depth and size." + suffix,
            prefix + productContext + " Aerial view from above, top-down, showing layout and shape." + suffix,
            prefix + productContext + " Children playing safely on the inflatable, joyful, family-friendly atmosphere." + suffix,
            prefix + productContext + " Close-up detail of inflatable material, stitching and structure, shallow depth of field." + suffix,
            prefix + productContext + " Wide shot in outdoor environment, park or garden, blue sky, inviting setting." + suffix
        ];
    }

    /// <summary>
    /// Gemini 2.5 Flash Image (2025/2026): generateContent + responseModalities IMAGE. API key ile, pay-as-you-go.
    /// </summary>
    private async Task<string?> GenerateOneImageViaGeminiAsync(string prompt, string apiKey, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{GeminiImageModel}:generateContent";

        var body = new
        {
            contents = new[] { new { parts = new[] { new { text = prompt } } } },
            generationConfig = new
            {
                responseModalities = new[] { "TEXT", "IMAGE" },
                responseMimeType = "text/plain"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Gemini image API error {StatusCode}: {Response}", response.StatusCode, responseText);
            var friendlyMessage = GetFriendlyErrorMessage(response.StatusCode, responseText);
            throw new InvalidOperationException(friendlyMessage);
        }

        return ParseImageFromGenerateContentResponse(responseText);
    }

    /// <summary>
    /// Imagen predict endpoint (faturalandırma gerekir). Admin ayarlarda "Imagen yedek kullan" açıksa kullanılır.
    /// </summary>
    private async Task<string?> GenerateOneImageViaImagenAsync(string prompt, string apiKey, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl}/{ImagenModel}:predict";

        var body = new
        {
            instances = new[] { new { prompt } },
            parameters = new
            {
                sampleCount = 1,
                aspectRatio = "1:1",
                personGeneration = "ALLOW_ADULT"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("x-goog-api-key", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var responseText = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Imagen API error {StatusCode}: {Response}", response.StatusCode, responseText);
            var friendlyMessage = GetFriendlyErrorMessage(response.StatusCode, responseText);
            throw new InvalidOperationException(friendlyMessage);
        }

        return ParseImageFromImagenResponse(responseText);
    }

    /// <summary>
    /// API hata yanıtından kullanıcıya gösterilecek Türkçe mesaj üretir.
    /// </summary>
    private static string GetFriendlyErrorMessage(System.Net.HttpStatusCode statusCode, string responseText)
    {
        try
        {
            using var doc = JsonDocument.Parse(responseText);
            if (doc.RootElement.TryGetProperty("error", out var err) && err.TryGetProperty("message", out var msg))
            {
                var message = msg.GetString() ?? "";
                if (message.Contains("billed users", StringComparison.OrdinalIgnoreCase))
                    return "Görsel üretimi şu an yalnızca faturalandırması açık hesaplara açıktır. Google AI Studio (aistudio.google.com) veya Google Cloud Console'da projenize fatura hesabı ekleyin: Ayarlar > Faturalandırma.";
                if (message.Contains("quota", StringComparison.OrdinalIgnoreCase) || message.Contains("RESOURCE_EXHAUSTED", StringComparison.OrdinalIgnoreCase))
                    return "Gemini/Imagen kotası aşıldı. Daha sonra tekrar deneyin veya faturalandırma sayfasından kotayı kontrol edin.";
                if (message.Contains("leaked", StringComparison.OrdinalIgnoreCase))
                    return "Bu API anahtarı güvenlik nedeniyle devre dışı bırakılmış (sızdı olarak işaretlenmiş). Google AI Studio (aistudio.google.com) üzerinden yeni bir API anahtarı oluşturup Admin → Genel Ayarlar → Yapay Zeka API Anahtarları bölümünden güncelleyin.";
                if (message.Contains("expired", StringComparison.OrdinalIgnoreCase) || message.Contains("renew", StringComparison.OrdinalIgnoreCase))
                    return "Gemini API anahtarının süresi dolmuş. Google AI Studio (aistudio.google.com) üzerinden yeni bir anahtar oluşturup Admin → Genel Ayarlar → Yapay Zeka API Anahtarları bölümünden güncelleyin.";
                if (message.Contains("API key", StringComparison.OrdinalIgnoreCase) || message.Contains("invalid", StringComparison.OrdinalIgnoreCase))
                    return "Geçersiz veya eksik Gemini API anahtarı. Admin → Genel Ayarlar → Yapay Zeka API Anahtarları bölümünden Google Gemini API anahtarını kontrol edin.";
                if (message.Contains("not found", StringComparison.OrdinalIgnoreCase) || message.Contains("404", StringComparison.OrdinalIgnoreCase))
                    return "Seçilen model bu API sürümünde bulunamıyor. Gemini 2.5 Flash Image (gemini-2.5-flash-image) veya Imagen kullanıldığından emin olun; gerekirse Google AI Studio'dan güncel model listesini kontrol edin.";
                return $"Gemini/Imagen API: {message}";
            }
        }
        catch (JsonException) { /* ignore */ }
        return $"Gemini/Imagen API hatası: {statusCode}. {responseText}";
    }

    /// <summary>
    /// generateContent yanıtından görsel çıkarır: candidates[].content.parts[].inlineData (data base64).
    /// </summary>
    private static string? ParseImageFromGenerateContentResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                return null;
            var content = candidates[0].GetProperty("content");
            if (!content.TryGetProperty("parts", out var parts))
                return null;
            foreach (var part in parts.EnumerateArray())
            {
                if (part.TryGetProperty("inlineData", out var inlineData))
                {
                    if (inlineData.TryGetProperty("data", out var data))
                    {
                        var b64 = data.GetString();
                        if (!string.IsNullOrEmpty(b64))
                            return b64;
                    }
                }
            }
        }
        catch (JsonException) { /* ignore */ }
        return null;
    }

    /// <summary>
    /// Imagen predict yanıtından görsel çıkarır.
    /// </summary>
    private static string? ParseImageFromImagenResponse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("predictions", out var predictions) && predictions.GetArrayLength() > 0)
            {
                var first = predictions[0];
                if (first.TryGetProperty("bytesBase64Encoded", out var b64))
                    return b64.GetString();
                if (first.TryGetProperty("image", out var img) && img.TryGetProperty("bytesBase64Encoded", out var imgB64))
                    return imgB64.GetString();
            }

            if (doc.RootElement.TryGetProperty("generatedImages", out var generated))
            {
                foreach (var item in generated.EnumerateArray())
                {
                    if (item.TryGetProperty("image", out var img))
                    {
                        if (img.TryGetProperty("imageBytes", out var bytes))
                        {
                            var str = bytes.GetString();
                            if (!string.IsNullOrEmpty(str))
                                return str;
                        }
                        if (img.TryGetProperty("bytesBase64Encoded", out var b64))
                            return b64.GetString();
                    }
                }
            }
        }
        catch (JsonException) { /* ignore */ }
        return null;
    }
}
