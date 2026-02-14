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

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };

            var httpClient = new HttpClient(handler)
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
                // Veritabanından güncel verileri al (cache kullanmaz)
                var products = await productRepository.GetAllForGoogleShoppingAsync();
                var googleShoppingProducts = new List<GoogleShoppingProduct>();

                foreach (var product in products)
                {
                    // GetAllForGoogleShoppingAsync zaten aktif ve fiyatı > 0 olan ürünleri getiriyor
                    
                    // Ürünün tüm resimlerini getir
                    var productImages = await productImageRepository.GetByProductIdAsync(product.Id);
                    var additionalImageLinks = new List<string>();
                    
                    foreach (var image in productImages.Where(img => !img.IsMainImage).OrderBy(img => img.DisplayOrder))
                    {
                        var imageUrl = GetImageUrl(image.LargePath);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            additionalImageLinks.Add(imageUrl);
                        }
                    }

                    var googleProduct = new GoogleShoppingProduct
                    {
                        Id = $"balonpark_{product.Id}",
                        OfferId = $"balonpark_offer_{product.Id}",
                        Title = product.Name ?? "Ürün",
                        Description = !string.IsNullOrEmpty(product.Description) ? product.Description : 
                                     (!string.IsNullOrEmpty(product.TechnicalDescription) ? product.TechnicalDescription : 
                                     product.Name ?? "Balon Park ürünü"),
                        Link = urlService.GetProductUrl(product.CategorySlug ?? "urunler", product.SubCategorySlug ?? "tum-urunler", product.Slug),
                        ImageLink = GetMainImageUrl(product),
                        AdditionalImageLinks = additionalImageLinks,
                        Availability = "in stock", // GetAllForGoogleShoppingAsync zaten aktif ürünleri getiriyor
                        Condition = "new",
                        Brand = "Balon Park",
                        Gtin = GenerateValidGtin(product.Id), // Doğrulanmış GTIN
                        Mpn = $"U-{product.Id}",
                        Price = product.Price,
                        Currency = "TRY", // Türkiye için TRY kullanılmalı
                        ContentLanguage = "tr",
                        TargetCountry = "TR", // Sadece Türkiye hedef ülke
                        GoogleProductCategory = GetGoogleProductCategory(product.CategoryId),
                        ProductType = $"{product.CategoryName} > {product.SubCategoryName}",
                        AgeGroup = "kids", // Şişme oyun grupları çocuklar için (Google geçerli değer: newborn, infant, toddler, kids, adult)
                        Gender = "unisex",
                        ItemGroupId = $"category_{product.CategoryId}",
                        ShippingWeight = "1 kg", // Varsayılan ağırlık
                        CustomLabel0 = product.CategoryName,
                        CustomLabel1 = product.SubCategoryName,
                        CustomLabel2 = $"Stok: {product.Stock}",
                        CustomLabel3 = !string.IsNullOrEmpty(product.Summary) ? product.Summary : null,
                        // Kargo bilgileri - Kargo para birimi Currency ile aynı olacak (TRY)
                        ShippingCountry = "TR",
                        ShippingService = "Standart Kargo",
                        ShippingPrice = 0 // Ücretsiz kargo (TRY para birimi ile)
                    };

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
            
            return new Google.Apis.ShoppingContent.v2_1.Data.Product
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
                Gtin = product.Gtin,
                Mpn = product.Mpn,
                Price = new Google.Apis.ShoppingContent.v2_1.Data.Price
                {
                    Value = formattedPrice, // TRY için TL cinsinden gönder (kuruş değil!)
                    Currency = product.Currency
                },
                ContentLanguage = product.ContentLanguage,
                TargetCountry = product.TargetCountry,
                GoogleProductCategory = product.GoogleProductCategory,
                AdditionalImageLinks = product.AdditionalImageLinks?.Any() == true ? product.AdditionalImageLinks.ToArray() : null,
                Color = product.Color,
                Material = product.Material,
                AgeGroup = product.AgeGroup,
                Gender = product.Gender,
                ItemGroupId = product.ItemGroupId,
                CustomLabel0 = product.CustomLabel0,
                CustomLabel1 = product.CustomLabel1,
                CustomLabel2 = product.CustomLabel2,
                CustomLabel3 = product.CustomLabel3,
                CustomLabel4 = product.CustomLabel4,
                // Zorunlu kanal alanı eklendi
                Channel = "online",
                // Kargo bilgileri
                Shipping = product.ShippingCountry != null && product.ShippingPrice.HasValue ? new List<Google.Apis.ShoppingContent.v2_1.Data.ProductShipping>
                {
                    new Google.Apis.ShoppingContent.v2_1.Data.ProductShipping
                    {
                        Country = product.ShippingCountry,
                        Service = product.ShippingService ?? "Standart",
                        Price = new Google.Apis.ShoppingContent.v2_1.Data.Price
                        {
                            Value = product.ShippingPrice.Value.ToString("F2", CultureInfo.InvariantCulture),
                            Currency = product.Currency
                        }
                    }
                } : null
            };
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
                Gtin = product.Gtin ?? string.Empty,
                Mpn = product.Mpn ?? string.Empty,
                Price = decimal.TryParse(product.Price?.Value, out var price) ? price : 0, // TL cinsinden değer
                Currency = product.Price?.Currency ?? "TRY",
                ContentLanguage = product.ContentLanguage ?? "tr",
                TargetCountry = product.TargetCountry ?? "TR",
                GoogleProductCategory = product.GoogleProductCategory ?? string.Empty,
                ProductType = string.Empty, // Google API'de bu alan bulunmuyor
                AdditionalImageLink = product.AdditionalImageLinks?.FirstOrDefault(),
                AdditionalImageLinks = product.AdditionalImageLinks?.ToList() ?? [],
                Color = product.Color,
                Material = product.Material,
                Size = string.Empty, // Google API'de bu alan bulunmuyor
                AgeGroup = product.AgeGroup,
                Gender = product.Gender,
                ItemGroupId = product.ItemGroupId,
                ShippingWeight = string.Empty, // Google API'de bu alan farklı şekilde
                ShippingLength = string.Empty,
                ShippingWidth = string.Empty,
                ShippingHeight = string.Empty,
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
                // Veritabanından Google Shopping için hazırlanmış ürünleri getir
                var products = await productRepository.GetAllForGoogleShoppingAsync();
                var googleShoppingProducts = new List<GoogleShoppingProduct>();

                foreach (var product in products)
                {
                    // Ürünün tüm resimlerini getir
                    var productImages = await productImageRepository.GetByProductIdAsync(product.Id);
                    var additionalImageLinks = new List<string>();
                    
                    foreach (var image in productImages.Where(img => !img.IsMainImage).OrderBy(img => img.DisplayOrder))
                    {
                        var imageUrl = GetImageUrl(image.LargePath);
                        if (!string.IsNullOrEmpty(imageUrl))
                        {
                            additionalImageLinks.Add(imageUrl);
                        }
                    }

                    var googleProduct = new GoogleShoppingProduct
                    {
                        Id = $"balonpark_{product.Id}",
                        OfferId = $"balonpark_offer_{product.Id}",
                        Title = product.Name ?? "Ürün",
                        Description = !string.IsNullOrEmpty(product.Description) ? product.Description : 
                                     (!string.IsNullOrEmpty(product.TechnicalDescription) ? product.TechnicalDescription : 
                                     product.Name ?? "Balon Park ürünü"),
                        Link = urlService.GetProductUrl(product.CategorySlug ?? "urunler", product.SubCategorySlug ?? "tum-urunler", product.Slug),
                        ImageLink = GetMainImageUrl(product),
                        AdditionalImageLinks = additionalImageLinks,
                        Availability = "in stock",
                        Condition = "new",
                        Brand = "Balon Park",
                        Gtin = GenerateValidGtin(product.Id),
                        Mpn = $"U-{product.Id}",
                        Price = product.Price,
                        Currency = "TRY", // Türkiye için TRY kullanılmalı
                        ContentLanguage = "tr",
                        TargetCountry = "TR", // Sadece Türkiye hedef ülke
                        GoogleProductCategory = GetGoogleProductCategory(product.CategoryId),
                        ProductType = $"{product.CategoryName} > {product.SubCategoryName}",
                        AgeGroup = "kids", // Google geçerli değer: newborn, infant, toddler, kids, adult
                        Gender = "unisex",
                        ItemGroupId = $"category_{product.CategoryId}",
                        ShippingWeight = "-",
                        CustomLabel0 = product.CategoryName,
                        CustomLabel1 = product.SubCategoryName,
                        CustomLabel2 = $"Stok: {product.Stock}",
                        CustomLabel3 = !string.IsNullOrEmpty(product.Summary) ? product.Summary : null,
                        // Kargo bilgileri - Kargo para birimi Currency ile aynı olacak (TRY)
                        ShippingCountry = "TR",
                        ShippingService = "Standart Kargo",
                        ShippingPrice = 0 // Ücretsiz kargo (TRY para birimi ile)
                    };

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

        private string GenerateValidGtin(int productId)
        {
            // GTIN-13 formatı için 12 basamak + 1 kontrol basamağı
            // Balon Park için prefix: 869 (Türkiye kodu)
            var prefix = "869";
            var productCode = productId.ToString("D6"); // 6 basamaklı ürün kodu
            var baseGtin = prefix + productCode; // 9 basamak
            
            // 13 basamak için 4 sıfır ekle (12 basamak + kontrol basamağı için)
            var gtin12 = baseGtin.PadRight(12, '0');
            
            // Kontrol basamağını hesapla
            var checkDigit = CalculateCheckDigit(gtin12);
            
            return gtin12 + checkDigit; // 13 basamaklı GTIN
        }
        
        private int CalculateCheckDigit(string gtin12)
        {
            // GTIN-13 kontrol basamağı hesaplama algoritması
            int sum = 0;
            for (int i = 0; i < 12; i++)
            {
                int digit = int.Parse(gtin12[i].ToString());
                // Çift pozisyonlarda 1, tek pozisyonlarda 3 ile çarp
                int multiplier = (i % 2 == 0) ? 1 : 3;
                sum += digit * multiplier;
            }
            
            // 10'a bölümünden kalanı 10'dan çıkar
            return (10 - (sum % 10)) % 10;
        }

        private string GetGoogleProductCategory(int? categoryId)
        {
            // Google Shopping kategori ID'leri - 2024 güncel kategoriler
            // Numeric ID kullanımı daha güvenli ve tutarlı
            // Kaynak: https://support.google.com/merchants/answer/6324436
            // Google Taxonomy: https://www.google.com/basepages/producttype/taxonomy-with-ids.en-US.txt
            return categoryId switch
            {
                1 => "1253", // Toys & Games (Şişme oyun grupları için genel kategori)
                2 => "1253", // Toys & Games (Şişme kaydıraklar için)
                3 => "1253", // Toys & Games (Şişme havuzlar için)
                4 => "1253", // Toys & Games (Şişme rodeo için)
                5 => "1253", // Toys & Games (Top havuzları için)
                6 => "1253", // Toys & Games (İç mekan oyun parkları için)
                7 => "1253", // Toys & Games (Trambolin için)
                8 => "1253", // Toys & Games (Softplay oyuncaklar için)
                9 => "96", // Party & Celebration > Party Supplies (Yer balonları)
                10 => "96", // Party & Celebration > Party Supplies (Balon tak)
                11 => "1253", // Toys & Games (Fly tüp)
                12 => "5192", // Apparel & Accessories > Costumes & Accessories (Şişme kostüm)
                13 => "96", // Party & Celebration > Party Supplies (Reklam balonları)
                _ => "1253" // Varsayılan: Toys & Games
            };
        }
    }
}