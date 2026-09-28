using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BalonPark.Data;
using BalonPark.Helpers;
using BalonPark.Models;

namespace BalonPark.Services.CatalogSync;

public interface ICatalogTranslationService
{
    Task<Category> TranslateCategoryAsync(Category source, string locale, CancellationToken cancellationToken = default);
    Task<SubCategory> TranslateSubCategoryAsync(SubCategory source, string locale, CancellationToken cancellationToken = default);
    Task<Product> TranslateProductAsync(Product source, string locale, CancellationToken cancellationToken = default);
}

public sealed class CatalogTranslationService(
    IHttpClientFactory httpClientFactory,
    SettingsRepository settingsRepository,
    ILogger<CatalogTranslationService> logger) : ICatalogTranslationService
{
    public const string HttpClientName = "CatalogSyncOpenAI";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<Category> TranslateCategoryAsync(Category source, string locale, CancellationToken cancellationToken = default)
    {
        if (SkipTranslation)
            return CloneCategory(source);

        var payload = new Dictionary<string, string?>
        {
            ["Name"] = source.Name,
            ["Description"] = source.Description,
            ["Slug"] = source.Slug
        };
        var translated = await TranslateFieldsAsync(payload, locale, "category", cancellationToken).ConfigureAwait(false);
        var clone = CloneCategory(source);
        clone.Name = Clip(translated.GetValueOrDefault("Name") ?? source.Name, Category.NameMaxLength) ?? source.Name;
        clone.Description = Clip(translated.GetValueOrDefault("Description") ?? source.Description, Category.DescriptionMaxLength);
        clone.Slug = EnsureLatinSlug(translated.GetValueOrDefault("Slug") ?? clone.Name, source.Id);
        return clone;
    }

    public async Task<SubCategory> TranslateSubCategoryAsync(SubCategory source, string locale, CancellationToken cancellationToken = default)
    {
        if (SkipTranslation)
            return CloneSubCategory(source);

        var payload = new Dictionary<string, string?>
        {
            ["Name"] = source.Name,
            ["Description"] = source.Description,
            ["Slug"] = source.Slug
        };
        var translated = await TranslateFieldsAsync(payload, locale, "subcategory", cancellationToken).ConfigureAwait(false);
        var clone = CloneSubCategory(source);
        clone.Name = Clip(translated.GetValueOrDefault("Name") ?? source.Name, SubCategory.NameMaxLength) ?? source.Name;
        clone.Description = Clip(translated.GetValueOrDefault("Description") ?? source.Description, SubCategory.DescriptionMaxLength);
        clone.Slug = EnsureLatinSlug(translated.GetValueOrDefault("Slug") ?? clone.Name, source.Id);
        return clone;
    }

    public async Task<Product> TranslateProductAsync(Product source, string locale, CancellationToken cancellationToken = default)
    {
        if (SkipTranslation)
            return CloneProduct(source);

        var payload = new Dictionary<string, string?>
        {
            ["Name"] = source.Name,
            ["Slug"] = source.Slug,
            ["Description"] = source.Description,
            ["TechnicalDescription"] = source.TechnicalDescription,
            ["Summary"] = source.Summary,
            ["WarrantyDescription"] = source.WarrantyDescription,
            ["AfterSalesService"] = source.AfterSalesService,
            ["FanDescription"] = source.FanDescription,
            ["ColorOptions"] = source.ColorOptions,
            ["DeliveryDays"] = source.DeliveryDays
        };
        var translated = await TranslateFieldsAsync(payload, locale, "product", cancellationToken).ConfigureAwait(false);
        var clone = CloneProduct(source);
        clone.Name = Clip(translated.GetValueOrDefault("Name") ?? source.Name, Product.NameMaxLength) ?? source.Name;
        clone.Description = translated.GetValueOrDefault("Description") ?? source.Description;
        clone.TechnicalDescription = translated.GetValueOrDefault("TechnicalDescription") ?? source.TechnicalDescription;
        clone.Summary = translated.GetValueOrDefault("Summary") ?? source.Summary;
        clone.WarrantyDescription = Clip(translated.GetValueOrDefault("WarrantyDescription") ?? source.WarrantyDescription, Product.WarrantyDescriptionMaxLength);
        clone.AfterSalesService = translated.GetValueOrDefault("AfterSalesService") ?? source.AfterSalesService;
        clone.FanDescription = Clip(translated.GetValueOrDefault("FanDescription") ?? source.FanDescription, Product.FanDescriptionMaxLength);
        clone.ColorOptions = translated.GetValueOrDefault("ColorOptions") ?? source.ColorOptions;
        clone.DeliveryDays = translated.GetValueOrDefault("DeliveryDays") ?? source.DeliveryDays;
        clone.Slug = EnsureLatinSlug(translated.GetValueOrDefault("Slug") ?? clone.Name, source.Id);
        return clone;
    }

    private static bool SkipTranslation
    {
        get
        {
            var v = Environment.GetEnvironmentVariable("CATALOG_SYNC_SKIP_TRANSLATION");
            return string.Equals(v, "1", StringComparison.Ordinal)
                   || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);
        }
    }

    private async Task<Dictionary<string, string?>> TranslateFieldsAsync(
        Dictionary<string, string?> fields,
        string locale,
        string entityKind,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, string?>(fields, StringComparer.Ordinal);
        var nonEmpty = fields
            .Where(kv => !string.IsNullOrWhiteSpace(kv.Value))
            .ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal);
        if (nonEmpty.Count == 0)
            return result;

        var apiKey = await GetApiKeyAsync().ConfigureAwait(false);
        var system = BuildSystemPrompt(locale);
        var user = $"Translate the following {entityKind} fields from Turkish. Return JSON with exactly the same keys.\n\n"
                   + JsonSerializer.Serialize(nonEmpty);

        var body = new
        {
            model = "gpt-4o-mini",
            temperature = 0.25,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user }
            }
        };

        var httpClient = httpClientFactory.CreateClient(HttpClientName);
        Exception? last = null;
        for (var attempt = 1; attempt <= 3; attempt++)
        {
            try
            {
                using var req = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

                using var resp = await httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
                var raw = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                if (!resp.IsSuccessStatusCode)
                {
                    // Do not retry auth failures.
                    if ((int)resp.StatusCode is 401 or 403)
                        throw new InvalidOperationException($"OpenAI HTTP {(int)resp.StatusCode}: invalid API key or forbidden.");
                    throw new InvalidOperationException($"OpenAI HTTP {(int)resp.StatusCode}: {raw[..Math.Min(raw.Length, 400)]}");
                }

                using var doc = JsonDocument.Parse(raw);
                var content = doc.RootElement
                    .GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString();

                if (string.IsNullOrWhiteSpace(content))
                    throw new InvalidOperationException("OpenAI returned empty content");

                var parsed = JsonSerializer.Deserialize<Dictionary<string, string?>>(content, JsonOptions)
                             ?? new Dictionary<string, string?>();

                foreach (var key in result.Keys.ToList())
                {
                    if (parsed.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                        result[key] = val;
                }
                return result;
            }
            catch (Exception ex) when (ex is not OperationCanceledException
                                       && ex.Message.Contains("invalid API key", StringComparison.OrdinalIgnoreCase) == false)
            {
                last = ex;
                logger.LogWarning(ex, "CatalogSync translation attempt {Attempt} failed for {Entity}", attempt, entityKind);
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(2 * attempt, 8)), cancellationToken).ConfigureAwait(false);
            }
        }

        throw new InvalidOperationException($"CatalogSync translation failed for {entityKind}/{locale}", last);
    }

    private async Task<string> GetApiKeyAsync()
    {
        var fromEnv = Environment.GetEnvironmentVariable("OPENAI_API_KEY")?.Trim()
                      ?? Environment.GetEnvironmentVariable("CatalogSync__OpenAiApiKey")?.Trim();
        if (!string.IsNullOrEmpty(fromEnv))
            return fromEnv;

        var settings = await settingsRepository.GetFirstAsync().ConfigureAwait(false);
        var key = settings?.ChatGPTApiKey?.Trim();
        if (string.IsNullOrEmpty(key) || LooksLikePlaceholder(key))
            throw new InvalidOperationException("ChatGPT API anahtarı tanımlı değil veya geçersiz (Admin → Ayarlar / OPENAI_API_KEY).");
        return key;
    }

    private static bool LooksLikePlaceholder(string key)
        => key.Length < 20
           || key.Contains("your-api-key", StringComparison.OrdinalIgnoreCase)
           || key.Equals("asdasd", StringComparison.OrdinalIgnoreCase)
           || key.Equals("test", StringComparison.OrdinalIgnoreCase);

    private static string BuildSystemPrompt(string locale)
    {
        var isGreek = locale.Equals("el", StringComparison.OrdinalIgnoreCase);
        var lang = isGreek ? "Greek" : "German";
        var brand = "Bala Park";
        var market = isGreek ? "Greece (balapark.gr)" : "Germany (balapark.de)";
        return $"""
            You are a professional e-commerce copywriter for inflatable playgrounds / bounce houses.
            Brand: {brand}. Market: {market}.
            Translate Turkish shop catalog text into natural, sales-ready {lang}.
            Rules:
            - Keep HTML tags intact. Do not translate URLs, emails, phones, codes, or measurements like 5x4x3m.
            - Replace Balon Park / Balapark / BalonPark in prose with {brand}.
            - Slug: lowercase a-z0-9 and hyphens only (transliterate; German ä→ae ö→oe ü→ue ß→ss; Greek to Latin).
            - Reply ONLY with a JSON object using the same keys as the input.
            """;
    }

    internal static string EnsureLatinSlug(string? text, int entityId)
    {
        var slug = SlugHelper.GenerateSlug(NormalizeForSlug(text ?? string.Empty));
        if (string.IsNullOrWhiteSpace(slug))
            slug = $"item-{entityId}";
        return slug.Length > 200 ? slug[..200] : slug;
    }

    private static string NormalizeForSlug(string text)
    {
        return text
            .Replace("ä", "ae", StringComparison.OrdinalIgnoreCase)
            .Replace("ö", "oe", StringComparison.OrdinalIgnoreCase)
            .Replace("ü", "ue", StringComparison.OrdinalIgnoreCase)
            .Replace("ß", "ss", StringComparison.Ordinal)
            .Replace("Ä", "ae")
            .Replace("Ö", "oe")
            .Replace("Ü", "ue");
    }

    private static string? Clip(string? value, int maxLen)
    {
        if (value is null) return null;
        var s = value.Trim();
        if (s.Length <= maxLen) return s;
        return s[..(maxLen - 1)].TrimEnd() + "…";
    }

    private static Category CloneCategory(Category s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Slug = s.Slug,
        Description = s.Description,
        IsActive = s.IsActive,
        DisplayOrder = s.DisplayOrder,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };

    private static SubCategory CloneSubCategory(SubCategory s) => new()
    {
        Id = s.Id,
        CategoryId = s.CategoryId,
        Name = s.Name,
        Slug = s.Slug,
        Description = s.Description,
        IsActive = s.IsActive,
        DisplayOrder = s.DisplayOrder,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };

    private static Product CloneProduct(Product s) => new()
    {
        Id = s.Id,
        CategoryId = s.CategoryId,
        SubCategoryId = s.SubCategoryId,
        Name = s.Name,
        Slug = s.Slug,
        Description = s.Description,
        TechnicalDescription = s.TechnicalDescription,
        Summary = s.Summary,
        Price = s.Price,
        UsdPrice = s.UsdPrice,
        EuroPrice = s.EuroPrice,
        RubPrice = s.RubPrice,
        Stock = s.Stock,
        DisplayOrder = s.DisplayOrder,
        IsActive = s.IsActive,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        ViewCount = s.ViewCount,
        InflatedLength = s.InflatedLength,
        InflatedWidth = s.InflatedWidth,
        InflatedHeight = s.InflatedHeight,
        UserCount = s.UserCount,
        AssemblyTime = s.AssemblyTime,
        RequiredPersonCount = s.RequiredPersonCount,
        FanDescription = s.FanDescription,
        FanWeightKg = s.FanWeightKg,
        PackagedLength = s.PackagedLength,
        PackagedDepth = s.PackagedDepth,
        PackagedWeightKg = s.PackagedWeightKg,
        PackagePalletCount = s.PackagePalletCount,
        HasCertificate = s.HasCertificate,
        WarrantyDescription = s.WarrantyDescription,
        AfterSalesService = s.AfterSalesService,
        IsDiscounted = s.IsDiscounted,
        IsPopular = s.IsPopular,
        IsProjectSpecial = s.IsProjectSpecial,
        DeliveryDays = s.DeliveryDays,
        DeliveryDaysMin = s.DeliveryDaysMin,
        DeliveryDaysMax = s.DeliveryDaysMax,
        IsFireResistant = s.IsFireResistant,
        MaterialWeight = s.MaterialWeight,
        MaterialWeightGrm2 = s.MaterialWeightGrm2,
        ColorOptions = s.ColorOptions,
        InflatedWeightKg = s.InflatedWeightKg
    };
}
