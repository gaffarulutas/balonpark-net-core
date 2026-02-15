using System.Globalization;
using System.Text;
using System.Xml;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Services;

/// <summary>
/// Yandex Market / Yandex Direct YML feed servisi.
/// Yandex 2026 gereksinimleri: benzersiz ID, yml_catalog tek kök, date YYYY-MM-DD hh:mm,
/// combined offer (name + vendor + typePrefix) ile daha iyi reklam eşleşmesi.
/// </summary>
public class YandexShoppingService(
    ProductRepository productRepository,
    ProductImageRepository productImageRepository,
    CategoryRepository categoryRepository,
    SubCategoryRepository subCategoryRepository,
    SettingsRepository settingsRepository,
    IYandexExchangeRateService exchangeRateService,
    IUrlService urlService,
    ILogger<YandexShoppingService> logger) : IYandexShoppingService
{
    private const string VendorName = "Balon Park";
    /// <summary>Yandex doğrulayıcı RUR, RUB, USD, BYR, BYN, KZT, EUR, UAH kabul ediyor. TCMB + CBR kurlarından TRY→RUB.</summary>
    private const string CurrencyId = "RUB";
    private const int YandexSubCategoryIdOffset = 10000;

    public async Task<string> GetYmlFeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = await settingsRepository.GetFirstAsync();
        var shopName = settings?.CompanyName ?? "Balon Park";
        var baseUrl = urlService.GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
            baseUrl = "https://www.balonpark.com";

        var categories = (await categoryRepository.GetAllAsync()).ToList();
        var subCategories = (await subCategoryRepository.GetAllAsync()).ToList();
        var products = (await productRepository.GetAllForGoogleShoppingAsync()).ToList();

        var tryToRubRate = await exchangeRateService.GetTryToRubRateAsync(cancellationToken);

        using var ms = new MemoryStream();
        var settingsXml = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            OmitXmlDeclaration = false,
            ConformanceLevel = ConformanceLevel.Document,
            Async = true
        };

        await using (var writer = XmlWriter.Create(ms, settingsXml))
        {
            // Yandex Webmaster best practice: tek kök yml_catalog, date YYYY-MM-DD HH:mm, UTF-8
            // Doğrulama: https://webmaster.yandex.com/tools/xml-validator/ → Şema olarak "Market" seçin (varsayılan "Doctors" yml_catalog tanımaz)
            await writer.WriteStartDocumentAsync(false);
            writer.WriteComment(" Yandex Webmaster XML validator: select schema 'Market' (online store). Default 'Doctors' does not declare yml_catalog. See https://yandex.com/support/webmaster/ ");
            await writer.WriteStartElementAsync(null, "yml_catalog", null);
            await writer.WriteAttributeStringAsync(null, "date", null, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

            await writer.WriteStartElementAsync(null, "shop", null);

            await WriteElementAsync(writer, "name", shopName);
            await WriteElementAsync(writer, "company", shopName);
            await WriteElementAsync(writer, "url", baseUrl.TrimEnd('/'));

            // Para birimi: Yandex doğrulayıcı sadece RUR, RUB, USD, BYR, BYN, KZT, EUR, UAH kabul ediyor
            await writer.WriteStartElementAsync(null, "currencies", null);
            await writer.WriteStartElementAsync(null, "currency", null);
            await writer.WriteAttributeStringAsync(null, "id", null, CurrencyId);
            await writer.WriteAttributeStringAsync(null, "rate", null, "1");
            await writer.WriteEndElementAsync();
            await writer.WriteEndElementAsync();

            // Kategoriler: önce ana kategoriler, sonra alt kategoriler (parentId ile)
            await writer.WriteStartElementAsync(null, "categories", null);
            foreach (var cat in categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Id))
            {
                await writer.WriteStartElementAsync(null, "category", null);
                await writer.WriteAttributeStringAsync(null, "id", null, cat.Id.ToString(CultureInfo.InvariantCulture));
                await writer.WriteStringAsync(cat.Name ?? "Kategori");
                await writer.WriteEndElementAsync();
            }
            foreach (var sub in subCategories.OrderBy(s => s.DisplayOrder).ThenBy(s => s.Id))
            {
                await writer.WriteStartElementAsync(null, "category", null);
                await writer.WriteAttributeStringAsync(null, "id", null, (YandexSubCategoryIdOffset + sub.Id).ToString(CultureInfo.InvariantCulture));
                await writer.WriteAttributeStringAsync(null, "parentId", null, sub.CategoryId.ToString(CultureInfo.InvariantCulture));
                await writer.WriteStringAsync(sub.Name ?? "Alt Kategori");
                await writer.WriteEndElementAsync();
            }
            await writer.WriteEndElementAsync();

            // Ürün teklifleri
            await writer.WriteStartElementAsync(null, "offers", null);

            foreach (var product in products)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await WriteOfferAsync(writer, product, baseUrl, tryToRubRate);
            }

            logger.LogDebug("Yandex YML feed generated: {ProductCount} offers", products.Count);

            await writer.WriteEndElementAsync(); // offers
            await writer.WriteEndElementAsync(); // shop
            await writer.WriteEndElementAsync(); // yml_catalog
            await writer.WriteEndDocumentAsync();
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private async Task WriteOfferAsync(XmlWriter writer, Product product, string baseUrl, decimal tryToRubRate)
    {
        // Yandex + Google aynı ID: farklı feed'lerde aynı ürün aynı ID (best practice 2026)
        var offerId = "balonpark_" + product.Id;

        await writer.WriteStartElementAsync(null, "offer", null);
        await writer.WriteAttributeStringAsync(null, "id", null, offerId);
        await writer.WriteAttributeStringAsync(null, "available", null, product.Stock > 0 ? "true" : "false");
        // Combined type: name + vendor + typePrefix (+ model) = daha iyi reklam eşleşmesi
        await writer.WriteAttributeStringAsync(null, "type", null, "vendor.model");

        var productUrl = urlService.GetProductUrl(
            product.CategorySlug ?? "urunler",
            product.SubCategorySlug ?? "tum-urunler",
            product.Slug);
        if (!productUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            productUrl = baseUrl.TrimEnd('/') + "/" + productUrl.TrimStart('/');

        // Zorunlu: name (full name), vendor, typePrefix, url, price, currencyId, categoryId
        var fullName = product.Name ?? "Ürün";
        await WriteElementAsync(writer, "name", fullName);
        await WriteElementAsync(writer, "vendor", VendorName);
        await WriteElementAsync(writer, "typePrefix", product.CategoryName ?? "Şişme Ürün");
        await WriteElementAsync(writer, "model", product.SubCategoryName ?? fullName);
        await WriteElementAsync(writer, "url", productUrl);

        // Fiyat: sitede TRY, feed'de RUB. TCMB + CBR kurlarından: price_RUB = price_TRY * tryToRubRate
        var priceRub = Math.Round(product.Price * tryToRubRate, 2);
        var price = priceRub.ToString("F2", CultureInfo.InvariantCulture);
        await WriteElementAsync(writer, "price", price);
        await WriteElementAsync(writer, "currencyId", CurrencyId);

        // categoryId: alt kategori (daha spesifik)
        var categoryId = YandexSubCategoryIdOffset + product.SubCategoryId;
        await WriteElementAsync(writer, "categoryId", categoryId.ToString(CultureInfo.InvariantCulture));

        // Resimler: en fazla 5 önerilir (Yandex)
        var pictureUrls = await GetPictureUrlsAsync(product);
        foreach (var pic in pictureUrls.Take(5))
            await WriteElementAsync(writer, "picture", pic);

        // Açıklama (reklam metni içermemeli)
        var description = SanitizeDescription(
            product.Description ?? product.TechnicalDescription ?? product.Summary ?? product.Name ?? "Balon Park ürünü");
        await WriteElementAsync(writer, "description", description);

        // İndirim: sitede eski fiyat gösteriliyorsa oldprice (fark en az %5 olmalı). RUB'ye çeviriyoruz
        if (product.IsDiscounted && product.Price > 0)
        {
            var oldPriceTry = Math.Round(product.Price / 0.85m, 2);
            if (oldPriceTry > product.Price && (oldPriceTry - product.Price) / oldPriceTry >= 0.05m)
            {
                var oldPriceRub = Math.Round(oldPriceTry * tryToRubRate, 2);
                await WriteElementAsync(writer, "oldprice", oldPriceRub.ToString("F2", CultureInfo.InvariantCulture));
            }
        }

        // Ödeme / sipariş notları
        var salesNotes = "Nakit, kredi kartı, havale/EFT ile ödeme. Sipariş onayı sonrası kargo.";
        if (!string.IsNullOrWhiteSpace(product.DeliveryDays))
            salesNotes += " Teslimat: " + product.DeliveryDays.Trim();
        await WriteElementAsync(writer, "sales_notes", salesNotes);

        // Parametreler (material, color, size, gender – eşleşme için)
        if (!string.IsNullOrWhiteSpace(product.ColorOptions))
            await WriteParamAsync(writer, "Renk", product.ColorOptions);
        if (!string.IsNullOrWhiteSpace(product.MaterialWeight))
            await WriteParamAsync(writer, "Malzeme", product.MaterialWeight);
        await WriteParamAsync(writer, "Cinsiyet", "unisex");
        await WriteParamAsync(writer, "Yaş grubu", "çocuk");

        // vendorCode, ülke (isteğe bağlı)
        await WriteElementAsync(writer, "vendorCode", "U-" + product.Id);
        await WriteElementAsync(writer, "country_of_origin", "Türkiye");

        await writer.WriteEndElementAsync(); // offer
    }

    private async Task<IReadOnlyList<string>> GetPictureUrlsAsync(Product product)
    {
        var list = new List<string>();
        if (!string.IsNullOrEmpty(product.MainImagePath))
            list.Add(urlService.GetImageUrl(product.MainImagePath));
        var images = await productImageRepository.GetByProductIdAsync(product.Id);
        foreach (var img in images.Where(i => !i.IsMainImage).OrderBy(i => i.DisplayOrder))
        {
            var url = urlService.GetImageUrl(img.LargePath ?? img.ThumbnailPath ?? "");
            if (!string.IsNullOrEmpty(url) && !list.Contains(url))
                list.Add(url);
        }
        if (list.Count == 0)
            list.Add(urlService.GetImageUrl("/assets/images/no-image.png"));
        return list;
    }

    private static string SanitizeDescription(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        // Reklam metni, link, rakip bilgisi kaldır (Yandex kuralları)
        var t = text.Trim();
        if (t.Length > 3000) t = t.Substring(0, 3000);
        return t;
    }

    private static async Task WriteElementAsync(XmlWriter writer, string localName, string? value)
    {
        if (value == null) return;
        await writer.WriteStartElementAsync(null, localName, null);
        await writer.WriteStringAsync(value);
        await writer.WriteEndElementAsync();
    }

    private static async Task WriteParamAsync(XmlWriter writer, string name, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        await writer.WriteStartElementAsync(null, "param", null);
        await writer.WriteAttributeStringAsync(null, "name", null, name);
        await writer.WriteStringAsync(value.Trim());
        await writer.WriteEndElementAsync();
    }
}
