using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.ShoppingContent.v2_1;
using Google.Apis.ShoppingContent.v2_1.Data;
using System.Globalization;
using BalonPark.Data;
using BalonPark.Models;

namespace BalonPark.Services
{
    public class GoogleShoppingService(
        IConfiguration configuration,
        ILogger<GoogleShoppingService> logger,
        ProductRepository productRepository,
        ProductImageRepository productImageRepository,
        SettingsRepository settingsRepository,
        IUrlService urlService) : IGoogleShoppingService
    {
        private ShoppingContentService? _shoppingService;
        private string? _merchantId;
        private readonly string _applicationName = configuration["GoogleShopping:ApplicationName"] ?? "BalonPark Shopping API";

        public async Task<bool> AuthenticateAsync()
        {
            try
            {
                var settings = await settingsRepository.GetFirstAsync();
                var merchantId = settings?.GoogleShoppingMerchantId ?? configuration["GoogleShopping:MerchantId"];
                var serviceAccountKeyJson = settings?.GoogleShoppingServiceAccountKeyJson;

                if (string.IsNullOrWhiteSpace(merchantId))
                {
                    logger.LogError("Google Shopping MerchantId not configured (check Admin Settings or appsettings.json)");
                    return false;
                }

                GoogleCredential credential;

                // 1. Önce Admin ayarlarındaki JSON key'i dene
                if (!string.IsNullOrWhiteSpace(serviceAccountKeyJson))
                {
                    try
                    {
                        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(serviceAccountKeyJson));
                        var serviceAccount = CredentialFactory.FromStream<ServiceAccountCredential>(ms);
                        credential = GoogleCredential.FromServiceAccountCredential(serviceAccount)
                            .CreateScoped(ShoppingContentService.Scope.Content);
                        logger.LogInformation("Google Shopping: Using credentials from Admin Settings");
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Invalid Google Shopping JSON key in Admin Settings");
                        return false;
                    }
                }
                else
                {
                    // 2. Fallback: appsettings.json'daki dosya yolu
                    var keyPath = configuration["GoogleShopping:ServiceAccountKeyPath"] ?? "";
                    var possiblePaths = new[]
                    {
                        Path.Combine(Directory.GetCurrentDirectory(), keyPath.Replace("~/", "")),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, keyPath.Replace("~/", "")),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Keys", "balonpark.json"),
                        Path.Combine(Directory.GetCurrentDirectory(), "Keys", "balonpark.json"),
                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", "Keys", "balonpark.json"),
                        Path.Combine(Directory.GetCurrentDirectory(), "bin", "Keys", "balonpark.json")
                    };

                    string? credentialPath = null;
                    foreach (var path in possiblePaths)
                    {
                        if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        {
                            credentialPath = path;
                            break;
                        }
                    }

                    if (credentialPath == null)
                    {
                        logger.LogError("Google Shopping: Service account key not found (add JSON in Admin Settings or place file in Keys/ folder)");
                        return false;
                    }

                    var serviceAccount = CredentialFactory.FromFile<ServiceAccountCredential>(credentialPath);
                    credential = GoogleCredential.FromServiceAccountCredential(serviceAccount)
                        .CreateScoped(ShoppingContentService.Scope.Content);
                    logger.LogInformation("Google Shopping: Using credentials from file {Path}", credentialPath);
                }

                _merchantId = merchantId;
                _shoppingService = new ShoppingContentService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = _applicationName
                });

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to authenticate with Google Shopping API");
                return false;
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(5)
            };

            httpClient.DefaultRequestHeaders.Add("User-Agent", "BalonPark-Shopping-API/1.0");

            return httpClient;
        }

        public async Task<string> CreateProductAsync(GoogleShoppingProduct product)
        {
            if (_shoppingService == null)
            {
                await AuthenticateAsync();
            }

            try
            {
                var productData = ConvertToGoogleProduct(product);
                var request = _shoppingService!.Products.Insert(productData, ulong.Parse(_merchantId!));
                var result = await request.ExecuteAsync();
                
                return result.Id?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to create product: {ProductId}", product.Id);
                throw;
            }
        }

        public async Task<string> UpdateProductAsync(GoogleShoppingProduct product)
        {
            if (_shoppingService == null)
            {
                await AuthenticateAsync();
            }

            try
            {
                var productData = ConvertToGoogleProduct(product);
                var request = _shoppingService!.Products.Update(productData, ulong.Parse(_merchantId!), product.Id);
                var result = await request.ExecuteAsync();
                
                return result.Id?.ToString() ?? string.Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update product: {ProductId}", product.Id);
                throw;
            }
        }

        public async Task<bool> DeleteProductAsync(string productId)
        {
            if (_shoppingService == null)
            {
                await AuthenticateAsync();
            }

            try
            {
                var request = _shoppingService!.Products.Delete(ulong.Parse(_merchantId!), productId);
                await request.ExecuteAsync();
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete product: {ProductId}", productId);
                return false;
            }
        }

        public async Task<List<GoogleShoppingProduct>> GetAllProductsAsync(bool includeInvalid = false)
        {
            if (_shoppingService == null)
            {
                await AuthenticateAsync();
            }

            try
            {
                var merchantId = ulong.Parse(_merchantId!);
                var request = _shoppingService!.Products.List(merchantId);
                request.MaxResults = 250;

                var products = new List<GoogleShoppingProduct>();
                ProductsListResponse? response;

                do
                {
                    response = await request.ExecuteAsync();
                    
                    if (response.Resources != null && response.Resources.Count > 0)
                    {
                        foreach (var product in response.Resources)
                        {
                            var convertedProduct = ConvertFromGoogleProduct(product);
                            products.Add(convertedProduct);
                        }
                    }
                    request.PageToken = response.NextPageToken;
                } while (!string.IsNullOrEmpty(response.NextPageToken));

                return products;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to retrieve products from Google Shopping");
                throw;
            }
        }

        public async Task<GoogleShoppingProduct?> GetProductAsync(string productId)
        {
            if (_shoppingService == null)
            {
                await AuthenticateAsync();
            }

            try
            {
                var request = _shoppingService!.Products.Get(ulong.Parse(_merchantId!), productId);
                var product = await request.ExecuteAsync();
                
                return ConvertFromGoogleProduct(product);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get product: {ProductId}", productId);
                return null;
            }
        }

        public async Task<bool> BatchInsertProductsAsync(List<GoogleShoppingProduct> products)
        {
            if (_shoppingService == null)
            {
                await AuthenticateAsync();
            }

            try
            {
                var batchRequest = new ProductsCustomBatchRequest();
                var entries = new List<ProductsCustomBatchRequestEntry>();

                foreach (var product in products)
                {
                    var entry = new ProductsCustomBatchRequestEntry
                    {
                        BatchId = entries.Count,
                        MerchantId = ulong.Parse(_merchantId!),
                        Method = "insert",
                        Product = ConvertToGoogleProduct(product)
                    };
                    entries.Add(entry);
                }

                batchRequest.Entries = entries;

                var request = _shoppingService!.Products.Custombatch(batchRequest);
                var response = await request.ExecuteAsync();
                
                if (response.Entries != null)
                {
                    var errorCount = response.Entries.Count(e => e.Errors != null);
                    if (errorCount > 0)
                    {
                        logger.LogWarning("Batch insert completed with {ErrorCount} errors out of {TotalCount} products", 
                            errorCount, response.Entries.Count);
                    }
                }
                
                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to batch insert products");
                return false;
            }
        }

        public async Task<bool> BatchUpdateProductsAsync(List<GoogleShoppingProduct> products)
        {
            if (_shoppingService == null)
            {
                await AuthenticateAsync();
            }

            try
            {
                var batchRequest = new ProductsCustomBatchRequest();
                var entries = new List<ProductsCustomBatchRequestEntry>();

                foreach (var product in products)
                {
                    var entry = new ProductsCustomBatchRequestEntry
                    {
                        BatchId = entries.Count,
                        MerchantId = ulong.Parse(_merchantId!),
                        Method = "update",
                        Product = ConvertToGoogleProduct(product)
                    };
                    entries.Add(entry);
                }

                batchRequest.Entries = entries;

                var request = _shoppingService!.Products.Custombatch(batchRequest);
                await request.ExecuteAsync();

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to batch update products");
                return false;
            }
        }

        public async Task<bool> BatchDeleteProductsAsync(List<string> productIds)
        {
            if (_shoppingService == null)
            {
                await AuthenticateAsync();
            }

            try
            {
                var batchRequest = new ProductsCustomBatchRequest();
                var entries = new List<ProductsCustomBatchRequestEntry>();

                foreach (var productId in productIds)
                {
                    var entry = new ProductsCustomBatchRequestEntry
                    {
                        BatchId = entries.Count,
                        MerchantId = ulong.Parse(_merchantId!),
                        Method = "delete",
                        ProductId = productId
                    };
                    entries.Add(entry);
                }

                batchRequest.Entries = entries;

                var request = _shoppingService!.Products.Custombatch(batchRequest);
                await request.ExecuteAsync();

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to batch delete products");
                return false;
            }
        }

        public async Task<List<GoogleShoppingProduct>> ConvertProductsToGoogleShoppingFormatAsync()
        {
            try
            {
                var products = await productRepository.GetAllForGoogleShoppingAsync();
                var googleShoppingProducts = new List<GoogleShoppingProduct>();

                foreach (var product in products)
                {
                    var googleProduct = await BuildGoogleShoppingProduct(product);
                    googleShoppingProducts.Add(googleProduct);
                }

                return googleShoppingProducts;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to convert products to Google Shopping format");
                throw;
            }
        }

        private Google.Apis.ShoppingContent.v2_1.Data.Product ConvertToGoogleProduct(GoogleShoppingProduct product)
        {
            var formattedPrice = product.Price.ToString("F2", CultureInfo.InvariantCulture);

            var googleProduct = new Google.Apis.ShoppingContent.v2_1.Data.Product
            {
                Id = product.Id,
                OfferId = product.OfferId,
                Title = product.Title,
                Description = product.Description,
                Link = product.Link,
                ImageLink = product.ImageLink,
                Availability = product.Availability,
                Condition = product.Condition,
                Brand = product.Brand,
                Mpn = product.Mpn,
                IdentifierExists = product.IdentifierExists,
                Price = new Google.Apis.ShoppingContent.v2_1.Data.Price
                {
                    Value = formattedPrice,
                    Currency = product.Currency
                },
                ContentLanguage = product.ContentLanguage,
                TargetCountry = product.TargetCountry,
                Channel = "online",
                GoogleProductCategory = product.GoogleProductCategory,
                ProductTypes = !string.IsNullOrEmpty(product.ProductType) ? new[] { product.ProductType } : null,
                AdditionalImageLinks = product.AdditionalImageLinks?.Count > 0 ? product.AdditionalImageLinks.ToArray() : null,
                Color = product.Color,
                Material = product.Material,
                AgeGroup = product.AgeGroup,
                Gender = product.Gender,
                ItemGroupId = product.ItemGroupId,
                ProductHighlights = product.ProductHighlights?.Count > 0 ? product.ProductHighlights : null,
                CustomLabel0 = product.CustomLabel0,
                CustomLabel1 = product.CustomLabel1,
                CustomLabel2 = product.CustomLabel2,
                CustomLabel3 = product.CustomLabel3,
                CustomLabel4 = product.CustomLabel4,
                Shipping = product.ShippingCountry != null && product.ShippingPrice.HasValue ? new List<Google.Apis.ShoppingContent.v2_1.Data.ProductShipping>
                {
                    new()
                    {
                        Country = product.ShippingCountry,
                        Service = product.ShippingService ?? "Standart Kargo",
                        Price = new Google.Apis.ShoppingContent.v2_1.Data.Price
                        {
                            Value = product.ShippingPrice.Value.ToString("F2", CultureInfo.InvariantCulture),
                            Currency = product.Currency
                        }
                    }
                } : null
            };

            if (!string.IsNullOrEmpty(product.Gtin))
            {
                googleProduct.Gtin = product.Gtin;
            }

            if (product.SalePrice.HasValue && product.SalePrice.Value > 0 && product.SalePrice.Value < product.Price)
            {
                googleProduct.SalePrice = new Google.Apis.ShoppingContent.v2_1.Data.Price
                {
                    Value = product.SalePrice.Value.ToString("F2", CultureInfo.InvariantCulture),
                    Currency = product.Currency
                };
            }

            return googleProduct;
        }

        private static GoogleShoppingProduct ConvertFromGoogleProduct(Google.Apis.ShoppingContent.v2_1.Data.Product product)
        {
            return new GoogleShoppingProduct
            {
                Id = product.Id ?? string.Empty,
                OfferId = product.OfferId ?? string.Empty,
                Title = product.Title ?? string.Empty,
                Description = product.Description ?? string.Empty,
                Link = product.Link ?? string.Empty,
                ImageLink = product.ImageLink ?? string.Empty,
                Availability = product.Availability ?? "in stock",
                Condition = product.Condition ?? "new",
                Brand = product.Brand ?? string.Empty,
                Gtin = product.Gtin,
                Mpn = product.Mpn,
                IdentifierExists = product.IdentifierExists ?? true,
                Price = decimal.TryParse(product.Price?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) ? price : 0,
                SalePrice = decimal.TryParse(product.SalePrice?.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var salePrice) ? salePrice : null,
                Currency = product.Price?.Currency ?? "TRY",
                ContentLanguage = product.ContentLanguage ?? "tr",
                TargetCountry = product.TargetCountry ?? "TR",
                GoogleProductCategory = product.GoogleProductCategory ?? string.Empty,
                ProductType = product.ProductTypes?.FirstOrDefault() ?? string.Empty,
                AdditionalImageLink = product.AdditionalImageLinks?.FirstOrDefault(),
                AdditionalImageLinks = product.AdditionalImageLinks?.ToList() ?? [],
                Color = product.Color,
                Material = product.Material,
                AgeGroup = product.AgeGroup,
                Gender = product.Gender,
                ItemGroupId = product.ItemGroupId,
                ProductHighlights = product.ProductHighlights?.ToList(),
                CustomLabel0 = product.CustomLabel0,
                CustomLabel1 = product.CustomLabel1,
                CustomLabel2 = product.CustomLabel2,
                CustomLabel3 = product.CustomLabel3,
                CustomLabel4 = product.CustomLabel4
            };
        }

        private string GetMainImageUrl(BalonPark.Models.Product product)
        {
            if (!string.IsNullOrEmpty(product.MainImagePath))
            {
                return urlService.GetImageUrl(product.MainImagePath);
            }

            // Varsayılan yer tutucu resim
            return urlService.GetImageUrl("/assets/images/no-image.png");
        }

        private string GetImageUrl(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return string.Empty;
            }

            return urlService.GetImageUrl(imagePath);
        }

        public async Task CheckProductStatusesAsync()
        {
            if (_shoppingService == null)
            {
                await AuthenticateAsync();
            }

            try
            {
                var products = await GetAllProductsAsync();
                
                if (products.Count == 0)
                {
                    logger.LogWarning("No products found in Google Shopping");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to check product statuses");
            }
        }

        public async Task<List<GoogleShoppingProduct>> GetProductsForApprovalAsync()
        {
            try
            {
                var products = await productRepository.GetAllForGoogleShoppingAsync();
                var googleShoppingProducts = new List<GoogleShoppingProduct>();

                foreach (var product in products)
                {
                    var googleProduct = await BuildGoogleShoppingProduct(product);
                    googleShoppingProducts.Add(googleProduct);
                }

                return googleShoppingProducts;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to prepare products for approval");
                throw;
            }
        }

        public async Task<bool> SubmitProductsForApprovalAsync(List<GoogleShoppingProduct> products)
        {
            try
            {
                var success = await BatchInsertProductsAsync(products);
                return success;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to submit products for approval");
                return false;
            }
        }

        public async Task<Dictionary<string, string>> GetProductApprovalStatusAsync()
        {
            try
            {
                var statusReport = new Dictionary<string, string>();
                
                // Google'dan mevcut ürünleri al
                var googleProducts = await GetAllProductsAsync();
                
                if (googleProducts.Count == 0)
                {
                    statusReport["status"] = "No products found in Google Merchant Center";
                    statusReport["message"] = "Products may still be processing or were rejected";
                    statusReport["recommendation"] = "Check Merchant Center for detailed status";
                }
                else
                {
                    statusReport["status"] = $"{googleProducts.Count} products found in Google Merchant Center";
                    statusReport["message"] = "Products are successfully uploaded";
                    statusReport["recommendation"] = "Check individual product status in Merchant Center";
                }
                
                return statusReport;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get product approval status");
                return new Dictionary<string, string>
                {
                    ["status"] = "Error",
                    ["message"] = ex.Message,
                    ["recommendation"] = "Check API connection and permissions"
                };
            }
        }

        /// <summary>
        /// Veritabanı ürününü Google Shopping formatına dönüştürür.
        /// Google Merchant Center best practices uygulanır:
        /// - Title maks 150 karakter
        /// - Description maks 5000 karakter, HTML temizlenir
        /// - CustomLabel alanları maks 100 karakter
        /// - Stok durumu gerçek stoktan kontrol edilir
        /// - Üretici GTIN yoksa IdentifierExists=false
        /// </summary>
        private async Task<GoogleShoppingProduct> BuildGoogleShoppingProduct(BalonPark.Models.Product product)
        {
            var productImages = await productImageRepository.GetByProductIdAsync(product.Id);
            var additionalImageLinks = productImages
                .Where(img => !img.IsMainImage)
                .OrderBy(img => img.DisplayOrder)
                .Select(img => GetImageUrl(img.LargePath))
                .Where(url => !string.IsNullOrEmpty(url))
                .Take(10) // Google maks 10 ek resim kabul eder
                .ToList();

            var title = BuildTitle(product);
            var description = BuildDescription(product);
            var availability = GetAvailability(product.Stock);
            var shippingWeight = GetShippingWeight(product);
            var productHighlights = BuildProductHighlights(product);

            return new GoogleShoppingProduct
            {
                Id = $"balonpark_{product.Id}",
                OfferId = $"balonpark_offer_{product.Id}",
                Title = title,
                Description = description,
                Link = urlService.GetProductUrl(
                    product.CategorySlug ?? "urunler",
                    product.SubCategorySlug ?? "tum-urunler",
                    product.Slug),
                ImageLink = GetMainImageUrl(product),
                AdditionalImageLinks = additionalImageLinks,
                Availability = availability,
                Condition = "new",
                Brand = "Balon Park",
                Gtin = null, // Üretici tarafından atanmış gerçek GTIN bulunmuyor
                Mpn = $"BP-{product.Id:D6}",
                IdentifierExists = false, // Özel üretim ürünler - gerçek GTIN/UPC yok
                Price = product.Price,
                Currency = "TRY",
                ContentLanguage = "tr",
                TargetCountry = "TR",
                GoogleProductCategory = GetGoogleProductCategory(product.CategoryId),
                ProductType = $"{product.CategoryName} > {product.SubCategoryName}",
                Color ="Kırmızı/Mavi/Yeşil/Sarı/Turuncu/Açık Mavi",
                AgeGroup = "kids",
                Gender = "unisex",
                ItemGroupId = $"category_{product.CategoryId}",
                ShippingWeight = shippingWeight,
                ProductHighlights = productHighlights,
                // Custom Labels: kampanya segmentasyonu için kısa etiketler (maks 100 karakter)
                CustomLabel0 = TruncateToCustomLabel(product.CategoryName),
                CustomLabel1 = TruncateToCustomLabel(product.SubCategoryName),
                CustomLabel2 = GetStockLabel(product.Stock),
                CustomLabel3 = GetPriceRangeLabel(product.Price),
                CustomLabel4 = product.IsPopular ? "Populer" : (product.IsDiscounted ? "Indirimli" : null),
                ShippingCountry = "TR",
                ShippingService = "Standart Kargo",
                ShippingPrice = 0 // Ucretsiz kargo
            };
        }
 
        /// <summary>
        /// Google Merchant Center spesifikasyonuna uygun title olusturur.
        /// Maks 150 karakter. Sadece urun adi kullanilir.
        /// </summary>
        private static string BuildTitle(BalonPark.Models.Product product)
        {
            var title = product.Name ?? "Ürün";

            if (title.Length > GoogleShoppingProduct.TitleMaxLength)
            {
                title = title[..(GoogleShoppingProduct.TitleMaxLength - 3)] + "...";
            }

            return title;
        }

        /// <summary>
        /// Google Merchant Center spesifikasyonuna uygun description olusturur.
        /// Maks 5000 karakter. HTML etiketleri temizlenir.
        /// Urun bilgileri, teknik ozellikler ve ozet birlestirilir.
        /// </summary>
        private static string BuildDescription(BalonPark.Models.Product product)
        {
            var parts = new List<string>();

            if (!string.IsNullOrEmpty(product.Description))
            {
                parts.Add(StripHtmlTags(product.Description));
            }

            if (!string.IsNullOrEmpty(product.TechnicalDescription))
            {
                parts.Add(StripHtmlTags(product.TechnicalDescription));
            }

            if (!string.IsNullOrEmpty(product.Summary) && parts.Count == 0)
            {
                parts.Add(StripHtmlTags(product.Summary));
            }

            var description = parts.Count > 0
                ? string.Join(" ", parts)
                : $"Balon Park {product.Name} - Kaliteli ve guvenli sisirilebilir oyun grubu.";

            if (description.Length > GoogleShoppingProduct.DescriptionMaxLength)
            {
                description = description[..(GoogleShoppingProduct.DescriptionMaxLength - 3)] + "...";
            }

            return description;
        }

        /// <summary>
        /// Urunun one cikan ozelliklerini (product_highlight) olusturur.
        /// Google maks 150 karakter/ozellik, 2-100 ozellik kabul eder.
        /// </summary>
        private static List<string>? BuildProductHighlights(BalonPark.Models.Product product)
        {
            var highlights = new List<string>();

            if (product.HasCertificate)
                highlights.Add("Uluslararasi guvenlik sertifikalari mevcut");

            if (product.IsFireResistant)
                highlights.Add("Yanmaz malzeme ile uretilmistir");

            if (!string.IsNullOrEmpty(product.WarrantyDescription))
                highlights.Add(TruncateText(product.WarrantyDescription, 150));

            if (!string.IsNullOrEmpty(product.MaterialWeight))
                highlights.Add($"Malzeme gramaji: {product.MaterialWeight}");

            if (product.UserCount.HasValue && product.UserCount > 0)
                highlights.Add($"Ayni anda {product.UserCount} cocuk kullanabilir");

            if (!string.IsNullOrEmpty(product.InflatedLength) && !string.IsNullOrEmpty(product.InflatedWidth))
                highlights.Add($"Sisik boyut: {product.InflatedLength} x {product.InflatedWidth} x {product.InflatedHeight}");

            return highlights.Count >= 2 ? highlights : null;
        }

        /// <summary>
        /// Stok durumunu Google Content API v2.1 formatina esler.
        /// Content API bosluklu degerler kabul eder: "in stock", "out of stock", "preorder", "backorder".
        /// Not: RSS/XML feed'lerde alt cizgili format kullanilir (in_stock, out_of_stock).
        /// </summary>
        private static string GetAvailability(int stock)
        {
            return stock > 0 ? "in stock" : "out of stock";
        }

        /// <summary>
        /// Kargo agirligini Google formatinda dondurur (ornek: "50 kg").
        /// Gecersiz veya eksik deger durumunda null dondurur.
        /// </summary>
        private static string? GetShippingWeight(BalonPark.Models.Product product)
        {
            if (product.PackagedWeightKg.HasValue && product.PackagedWeightKg > 0)
            {
                return $"{product.PackagedWeightKg.Value:F1} kg";
            }

            if (product.InflatedWeightKg.HasValue && product.InflatedWeightKg > 0)
            {
                return $"{product.InflatedWeightKg.Value:F1} kg";
            }

            return null;
        }

        /// <summary>
        /// Stok segmentasyonu icin etiket olusturur.
        /// Kampanya teklif stratejisi ve raporlamada kullanilir.
        /// </summary>
        private static string GetStockLabel(int stock)
        {
            return stock switch
            {
                0 => "Tukendi",
                <= 5 => "Sinirli Stok",
                <= 20 => "Stokta",
                _ => "Yuksek Stok"
            };
        }

        /// <summary>
        /// Fiyat araliklarini segmentler halinde etiketler.
        /// Google Ads kampanyalarinda fiyat bazli teklif stratejisi icin kullanilir.
        /// </summary>
        private static string GetPriceRangeLabel(decimal price)
        {
            return price switch
            {
                < 10_000 => "0-10K TL",
                < 50_000 => "10K-50K TL",
                < 100_000 => "50K-100K TL",
                < 250_000 => "100K-250K TL",
                _ => "250K+ TL"
            };
        }

        /// <summary>
        /// Metni Google custom_label limiti olan 100 karaktere kisaltir.
        /// Merchant Center hesabindaki her custom label icin maks 1000 benzersiz deger kullanilmalidir.
        /// </summary>
        private static string? TruncateToCustomLabel(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return null;

            return text.Length <= GoogleShoppingProduct.CustomLabelMaxLength
                ? text
                : text[..(GoogleShoppingProduct.CustomLabelMaxLength - 3)] + "...";
        }

        /// <summary>Metni belirtilen uzunluga kisaltir.</summary>
        private static string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
                return text ?? string.Empty;

            return text[..(maxLength - 3)] + "...";
        }

        /// <summary>HTML etiketlerini temizler, yalnizca duz metin dondurur.</summary>
        private static string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html))
                return string.Empty;

            var text = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", " ");
            text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ");
            return text.Trim();
        }

        private string GetGoogleProductCategory(int? categoryId)
        {
            // Google Shopping Taxonomy (numeric ID) - daha guvenli ve tutarli
            // https://www.google.com/basepages/producttype/taxonomy-with-ids.en-US.txt
            return categoryId switch
            {
                1 => "1253",  // Toys & Games (Sisme oyun gruplari)
                2 => "1253",  // Toys & Games (Sisme kaydıraklar)
                3 => "1253",  // Toys & Games (Sisme havuzlar)
                4 => "1253",  // Toys & Games (Sisme rodeo)
                5 => "1253",  // Toys & Games (Top havuzlari)
                6 => "1253",  // Toys & Games (Ic mekan oyun parklari)
                7 => "1253",  // Toys & Games (Trambolin)
                8 => "1253",  // Toys & Games (Softplay oyuncaklar)
                9 => "96",    // Party Supplies (Yer balonlari)
                10 => "96",   // Party Supplies (Balon tak)
                11 => "1253", // Toys & Games (Fly tup)
                12 => "5192", // Costumes & Accessories (Sisme kostum)
                13 => "96",   // Party Supplies (Reklam balonlari)
                _ => "1253"   // Varsayilan: Toys & Games
            };
        }
    }
}