using HtmlAgilityPack;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using System.Globalization;
using BalonPark.Data;

namespace BalonPark.Services;

public class PdfService(
    ILogger<PdfService> logger, 
    IUrlService urlService, 
    SettingsRepository settingsRepository,
    IWebHostEnvironment environment)
{
    static PdfService()
    {
        // Inter font ailesini kaydet
        try
        {
            var interRegularPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "fonts", "Inter", "static", "Inter_18pt-Regular.ttf");
            var interBoldPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "fonts", "Inter", "static", "Inter_18pt-Bold.ttf");

            if (File.Exists(interRegularPath))
            {
                FontFactory.Register(interRegularPath, "Inter-Regular");
                Console.WriteLine($"Inter Regular font kaydedildi: {interRegularPath}");
            }
            if (File.Exists(interBoldPath))
            {
                FontFactory.Register(interBoldPath, "Inter-Bold");
                Console.WriteLine($"Inter Bold font kaydedildi: {interBoldPath}");
            }
        }
        catch
        {
            // Inter font kayıt edilemezse varsayılan fontları kullan
        }
    }
    public async Task<byte[]> GeneratePdfFromHtmlAsync(string htmlContent, string fileName = "document.pdf")
    {
        try
        {
            logger.LogInformation("PDF oluşturma başlıyor. HTML uzunluğu: {Length}", htmlContent?.Length ?? 0);
            logger.LogInformation("HTML içeriği (ilk 500 karakter): {Content}", htmlContent?.Substring(0, Math.Min(500, htmlContent?.Length ?? 0)) ?? "");

            // Chrome desteği olmadığı için direkt iTextSharp kullan
            return await GeneratePdfWithiTextSharpAsync(htmlContent ?? "", fileName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "iTextSharp hatası, basit PDF deneniyor");
            try
            {
                return await GenerateSimplePdfAsync(htmlContent, fileName);
            }
            catch (Exception simpleEx)
            {
                logger.LogError(simpleEx, "PDF oluşturulurken hata oluştu: {FileName}", fileName);
                throw new Exception($"PDF oluşturulurken hata oluştu: {simpleEx.Message}", simpleEx);
            }
        }
    }



    private Font GetTurkishFont(int size, int style = Font.NORMAL, BaseColor? color = null)
    {
        try
        {
            // Inter font kullan (Türkçe karakter desteği ile)
            if (style == Font.BOLD)
            {
                var font = FontFactory.GetFont("Inter-Bold", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, size, style, color ?? BaseColor.BLACK);
                logger.LogDebug("Inter-Bold font kullanıldı, boyut: {Size}", size);
                return font;
            }
            else
            {
                var font = FontFactory.GetFont("Inter-Regular", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, size, style, color ?? BaseColor.BLACK);
                logger.LogDebug("Inter-Regular font kullanıldı, boyut: {Size}", size);
                return font;
            }
        }
        catch
        {
            try
            {
                // Fallback: Arial ile
                return FontFactory.GetFont("Arial", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, size, style, color ?? BaseColor.BLACK);
            }
            catch
            {
                try
                {
                    // Fallback: Helvetica ile
                    return FontFactory.GetFont("Helvetica", BaseFont.IDENTITY_H, BaseFont.EMBEDDED, size, style, color ?? BaseColor.BLACK);
                }
                catch
                {
                    // Son çare: varsayılan font
                    return FontFactory.GetFont(FontFactory.HELVETICA, size, style, color ?? BaseColor.BLACK);
                }
            }
        }
    }

    private void AddPlaceholderImage(PdfPCell cell)
    {
        try
        {
            var noImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "images", "no-image.png");
            if (File.Exists(noImagePath))
            {
                var placeholderImage = iTextSharp.text.Image.GetInstance(noImagePath);
                placeholderImage.ScaleToFit(50, 50); // Placeholder boyutunu ayarla
                cell.AddElement(placeholderImage);
                logger.LogInformation("Placeholder resim başarıyla eklendi");
            }
            else
            {
                // Placeholder resim bulunamazsa metin ekle
                var cellFont = GetTurkishFont(8, Font.NORMAL, BaseColor.GRAY);
                cell.AddElement(new Phrase("Resim Yok", cellFont));
                logger.LogWarning("Placeholder resim bulunamadı: {Path}", noImagePath);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Placeholder resim yüklenirken hata, metin kullanılıyor");
            var cellFont = GetTurkishFont(8, Font.NORMAL, BaseColor.GRAY);
            cell.AddElement(new Phrase("Resim Yok", cellFont));
        }
    }

    private void AddHeaderToDocument(Document document)
    {
        try
        {
            // Settings'i cache'den al
            var settings = settingsRepository.GetFirstAsync().GetAwaiter().GetResult();
            var companyName = settings?.CompanyName ?? "ÜNLÜ PARK";
            
            // Header için tablo oluştur - Logo sol, İletişim sağ
            var headerTable = new PdfPTable(2)
            {
                WidthPercentage = 100,
                SpacingBefore = 0,
                SpacingAfter = 15
            };
            // Logo %40, İletişim %60 genişlik
            headerTable.SetWidths([0.4f, 0.6f]);

            // Logo hücresi (sol taraf) - Best practice alignment
            var logoCell = new PdfPCell
            {
                Border = Rectangle.NO_BORDER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 5
            };

            try
            {
                // Logo path'i Settings'ten al
                string? logoPath = null;
                
                if (!string.IsNullOrEmpty(settings?.Logo))
                {
                    // Settings'te logo varsa onu kullan
                    logoPath = Path.Combine(environment.WebRootPath, settings.Logo.TrimStart('~', '/'));
                }
                else
                {
                    // Varsayılan logo
                    logoPath = Path.Combine(environment.WebRootPath, "assets", "images", "logo", "logo.png");
                }
                
                if (File.Exists(logoPath))
                {
                    var logo = iTextSharp.text.Image.GetInstance(logoPath);
                    logo.ScaleToFit(200, 100); // Logo boyutunu büyüt - daha profesyonel
                    logoCell.AddElement(logo);
                    logger.LogInformation("Logo başarıyla yüklendi: {Path}", logoPath);
                }
                else
                {
                    // Logo yoksa metin ekle
                    logger.LogWarning("Logo dosyası bulunamadı: {Path}", logoPath);
                    var logoTextFont = GetTurkishFont(14, Font.BOLD, BaseColor.DARK_GRAY);
                    logoCell.AddElement(new Paragraph(companyName, logoTextFont));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Logo yüklenirken hata, metin kullanılıyor");
                var logoTextFont = GetTurkishFont(14, Font.BOLD, BaseColor.DARK_GRAY);
                logoCell.AddElement(new Paragraph(companyName, logoTextFont));
            }

            headerTable.AddCell(logoCell);

            // İletişim bilgileri hücresi (sağ taraf) - Best practice styling
            var contactCell = new PdfPCell
            {
                Border = Rectangle.NO_BORDER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Padding = 5
            };

            // İletişim bilgileri için modern font hiyerarşisi
            var contactTitleFont = GetTurkishFont(10, Font.BOLD, BaseColor.DARK_GRAY);
            var contactFont = GetTurkishFont(9, Font.NORMAL, BaseColor.DARK_GRAY);
            var contactSmallFont = GetTurkishFont(8, Font.NORMAL, BaseColor.GRAY);

            var contactInfo = new Paragraph();
            
            // İletişim bilgilerini sağa dayalı yapmak için alignment ekle
            contactInfo.Alignment = Element.ALIGN_RIGHT;
            
            // İletişim bilgileri - Settings'ten al
            if (!string.IsNullOrEmpty(settings?.Address))
            {
                contactInfo.Add(new Chunk($"📍 {settings.Address}\n", contactFont));
                if (!string.IsNullOrEmpty(settings.City))
                {
                    var cityText = settings.City;
                    if (!string.IsNullOrEmpty(settings.District))
                        cityText = $"{settings.District}/{cityText}";
                    if (!string.IsNullOrEmpty(settings.PostalCode))
                        cityText = $"{settings.PostalCode} {cityText}";
                    contactInfo.Add(new Chunk($"   {cityText}\n", contactSmallFont));
                }
            }
            
            if (!string.IsNullOrEmpty(settings?.PhoneNumber))
            {
                contactInfo.Add(new Chunk($"📞 {settings.PhoneNumber}\n", contactFont));
            }
            
            if (!string.IsNullOrEmpty(settings?.Email))
            {
                contactInfo.Add(new Chunk($"✉️ {settings.Email}\n", contactFont));
            }
            
            contactCell.AddElement(contactInfo);

            headerTable.AddCell(contactCell);

            document.Add(headerTable);

            // Alt çizgi ekle
            var line = new LineSeparator();
            document.Add(new Chunk(line));
            document.Add(new Paragraph(" ")); // Boş satır
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Header eklenirken hata oluştu");
        }
    }


    private Task<byte[]> GeneratePdfWithiTextSharpAsync(string htmlContent, string fileName)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 20, 20, 20, 20);
            var writer = PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Header - Logo ve iletişim bilgileri
            AddHeaderToDocument(document);

            // HTML içeriğinden ürün bilgilerini parse et
            var products = ParseProductsFromHtml(htmlContent);
            logger.LogInformation("Parse edilen ürün sayısı: {Count}", products.Count);

            if (products.Count == 0)
            {
                logger.LogWarning("Hiç ürün bulunamadı, boş PDF oluşturuluyor");
                // Boş ürün mesajı ekle
                var emptyFont = GetTurkishFont(12, Font.NORMAL, BaseColor.GRAY);
                var emptyMessage = new Paragraph("Ürün bulunamadı", emptyFont)
                {
                    Alignment = Element.ALIGN_CENTER
                };
                document.Add(emptyMessage);
            }
            else
            {
                // Tablo oluştur
                var table = new PdfPTable(7)
                {
                    WidthPercentage = 100,
                    HeaderRows = 1 // Header satırlarının sayfa geçişlerinde tekrarlanması için
                }; // Resim, Kod, Ad, Ölçü, Fiyat TL, Fiyat USD, Fiyat EUR
                table.SetWidths([1f, 1f, 2.5f, 1.2f, 1.2f, 1.2f, 1.2f]);

                // Tablo başlıkları - Türkçe font kullan
                var headerFont = GetTurkishFont(10, Font.BOLD, BaseColor.WHITE);
                var headerCell = new PdfPCell(new Phrase("Resim", headerFont))
                {
                    BackgroundColor = new BaseColor(98, 98, 166),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(headerCell);

                headerCell = new PdfPCell(new Phrase("Kod", headerFont))
                {
                    BackgroundColor = new BaseColor(98, 98, 166),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(headerCell);

                headerCell = new PdfPCell(new Phrase("Ürün Adı", headerFont))
                {
                    BackgroundColor = new BaseColor(98, 98, 166),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(headerCell);

                headerCell = new PdfPCell(new Phrase("Ölçü", headerFont))
                {
                    BackgroundColor = new BaseColor(98, 98, 166),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(headerCell);

                headerCell = new PdfPCell(new Phrase("Fiyat (TL)", headerFont))
                {
                    BackgroundColor = new BaseColor(98, 98, 166),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(headerCell);

                headerCell = new PdfPCell(new Phrase("Fiyat (USD)", headerFont))
                {
                    BackgroundColor = new BaseColor(98, 98, 166),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(headerCell);

                headerCell = new PdfPCell(new Phrase("Fiyat (EURO)", headerFont))
                {
                    BackgroundColor = new BaseColor(98, 98, 166),
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(headerCell);

                // Ürün satırları - Türkçe font kullan
                var cellFont = GetTurkishFont(9);
                var priceFont = GetTurkishFont(9, Font.BOLD, BaseColor.DARK_GRAY);

                foreach (var product in products)
                {
                    // Resim hücresi
                    var cell = new PdfPCell
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5
                    };

                    if (!string.IsNullOrEmpty(product.ImageBase64))
                    {
                        try
                        {
                            logger.LogInformation("Ürün {ProductId} için resim ekleniyor, base64 uzunluk: {Length}", product.Id, product.ImageBase64.Length);
                            var imageBytes = Convert.FromBase64String(product.ImageBase64);
                            var image = iTextSharp.text.Image.GetInstance(imageBytes);
                            image.ScaleToFit(50, 50); // Resim boyutunu ayarla
                            cell.AddElement(image);
                            logger.LogInformation("Ürün {ProductId} resmi başarıyla eklendi", product.Id);
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Resim yüklenirken hata: {ProductId}, placeholder kullanılıyor", product.Id);
                            AddPlaceholderImage(cell);
                        }
                    }
                    else
                    {
                        logger.LogInformation("Ürün {ProductId} için resim yok, placeholder kullanılıyor", product.Id);
                        AddPlaceholderImage(cell);
                    }

                    table.AddCell(cell);

                    // Kod - tıklanabilir link ile
                    var codePhrase = new Phrase();
                    // Gerçek ürün URL'sini oluştur (category/subcategory/product slug)
                    var codeUrl = urlService.GetProductUrl(product.CategorySlug ?? "urunler", product.SubCategorySlug ?? "tum-urunler", product.Slug);
                    
                    var codeLink = new Chunk($"U-{product.Id}", GetTurkishFont(9, Font.UNDERLINE, BaseColor.BLUE));
                    codeLink.SetAction(new PdfAction(codeUrl));
                    codePhrase.Add(codeLink);
                    
                    cell = new PdfPCell(codePhrase)
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 5
                    };
                    table.AddCell(cell);

                // Ürün adı - tıklanabilir link ile
                var productNamePhrase = new Phrase();
                
                // Gerçek ürün URL'sini oluştur (category/subcategory/product slug)
                var productUrl = urlService.GetProductUrl(product.CategorySlug ?? "urunler", product.SubCategorySlug ?? "tum-urunler", product.Slug);
                
                // Tıklanabilir link ekle
                var productLink = new Chunk(product.Name, GetTurkishFont(9, Font.UNDERLINE, BaseColor.BLUE));
                productLink.SetAction(new PdfAction(productUrl));
                
                productNamePhrase.Add(productLink);
                
                cell = new PdfPCell(productNamePhrase)
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(cell);

                // Ölçü
                cell = new PdfPCell(new Phrase(product.Dimensions ?? "Belirtilmemiş", cellFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(cell);

                // Fiyat TL
                cell = new PdfPCell(new Phrase($"{product.Price:N2}", priceFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(cell);

                // Fiyat USD
                cell = new PdfPCell(new Phrase($"{product.UsdPrice:N2}", priceFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(cell);

                // Fiyat EUR
                cell = new PdfPCell(new Phrase($"{product.EuroPrice:N2}", priceFont))
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = 5
                };
                table.AddCell(cell);
                }

                document.Add(table);
            }

            document.Close();

            var pdfBytes = memoryStream.ToArray();
            logger.LogInformation("PDF oluşturuldu. Boyut: {Size} bytes", pdfBytes.Length);
            return Task.FromResult(pdfBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "iTextSharp ile PDF oluşturma hatası");
            throw new Exception($"PDF oluşturulurken hata oluştu: {ex.Message}", ex);
        }
    }

    private List<ProductInfo> ParseProductsFromHtml(string htmlContent)
    {
        var products = new List<ProductInfo>();

        try
        {
            logger.LogInformation("HTML parsing başlıyor. HTML uzunluğu: {Length}", htmlContent?.Length ?? 0);

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent ?? "");

            // Tüm tabloları kontrol et
            var allTables = doc.DocumentNode.SelectNodes("//table");
            logger.LogInformation("HTML'de {Count} tablo bulundu", allTables?.Count ?? 0);

            var tableRows = doc.DocumentNode.SelectNodes("//table[@class='product-table']//tbody//tr");
            logger.LogInformation("product-table class'ına sahip tabloda {Count} satır bulundu", tableRows?.Count ?? 0);

            // Eğer product-table bulunamazsa tüm tabloları kontrol et
            if (tableRows == null || tableRows.Count == 0)
            {
                logger.LogWarning("product-table bulunamadı, tüm tablolar kontrol ediliyor");
                tableRows = doc.DocumentNode.SelectNodes("//table//tr");
                logger.LogInformation("Tüm tablolarda toplam {Count} satır bulundu", tableRows?.Count ?? 0);
            }

            foreach (var row in tableRows ?? Enumerable.Empty<HtmlNode>())
            {
                var cells = row.SelectNodes("td");
                logger.LogInformation("Satırda {CellCount} hücre bulundu", cells?.Count ?? 0);
                if (cells == null || cells.Count < 7)
                {
                    logger.LogWarning("Satırda yeterli hücre yok (minimum 7 gerekli), atlanıyor");
                    continue; // Resim, Kod, Ad, Ölçü, TL, USD, EUR
                }
                var product = new ProductInfo();
                
                // HTML data attribute'larından gerçek slug bilgilerini al
                var productIdAttr = row.GetAttributeValue("data-product-id", "");
                var productSlugAttr = row.GetAttributeValue("data-product-slug", "");
                var categorySlugAttr = row.GetAttributeValue("data-category-slug", "");
                var subCategorySlugAttr = row.GetAttributeValue("data-subcategory-slug", "");
                
                if (!string.IsNullOrEmpty(productIdAttr) && int.TryParse(productIdAttr, out var productId))
                {
                    product.Id = productId;
                }
                
                if (!string.IsNullOrEmpty(productSlugAttr))
                {
                    product.Slug = productSlugAttr;
                }
                
                if (!string.IsNullOrEmpty(categorySlugAttr))
                {
                    product.CategorySlug = categorySlugAttr;
                }
                
                if (!string.IsNullOrEmpty(subCategorySlugAttr))
                {
                    product.SubCategorySlug = subCategorySlugAttr;
                }

                // Resim hücresi (1. hücre) - img tag'inden src attribute'unu çıkar
                var imageCell = cells[0];
                var imgTag = imageCell.SelectSingleNode(".//img");

                if (imgTag != null)
                {
                    var srcAttribute = imgTag.GetAttributeValue("src", "");
                    if (!string.IsNullOrEmpty(srcAttribute) && srcAttribute.StartsWith("data:image") && srcAttribute.Contains("base64,"))
                    {
                        var base64Index = srcAttribute.IndexOf("base64,", StringComparison.Ordinal) + 7;
                        product.ImageBase64 = srcAttribute[base64Index..];
                    }
                }

                // Kod hücresi (2. hücre) - ID zaten data attribute'dan alındı
                var codeCell = cells[1];
                var codeText = codeCell.InnerText.Trim();
                
                // Eğer data attribute'dan ID alınamadıysa, kod hücresinden al
                if (product.Id == 0 && codeText.StartsWith("U-") && int.TryParse(codeText.AsSpan(2), out var id))
                {
                    product.Id = id;
                }

                // Ürün adı (3. hücre)
                product.Name = cells[2].InnerText.Trim();
                
                // Eğer data attribute'lardan slug bilgileri alınamadıysa varsayılan değerler
                if (string.IsNullOrEmpty(product.Slug))
                {
                    product.Slug = product.Name.ToLowerInvariant()
                        .Replace(" ", "-")
                        .Replace("ç", "c")
                        .Replace("ğ", "g")
                        .Replace("ı", "i")
                        .Replace("ö", "o")
                        .Replace("ş", "s")
                        .Replace("ü", "u")
                        .Replace("(", "")
                        .Replace(")", "")
                        .Replace("'", "")
                        .Replace("\"", "");
                }
                
                if (string.IsNullOrEmpty(product.CategorySlug))
                {
                    product.CategorySlug = "urunler";
                }
                
                if (string.IsNullOrEmpty(product.SubCategorySlug))
                {
                    product.SubCategorySlug = "tum-urunler";
                }

                // Ölçü (4. hücre)
                product.Dimensions = cells[3].InnerText.Trim();

                // Fiyat TL (5. hücre) - Metin formatında parsing
                var priceText = cells[4].InnerText.Trim();
                logger.LogInformation("TL Fiyat parsing: '{PriceText}'", priceText);

                // Türkçe fiyat formatını temizle (TL, ₺, virgül, nokta)
                var cleanPriceText = priceText.Replace("TL", "").Replace("₺", "").Replace(" ", "").Trim();

                // Türkçe ondalık ayırıcı (virgül) -> nokta dönüşümü
                cleanPriceText = cleanPriceText.Replace(",", ".");

                // Binlik ayırıcı noktaları kaldır (sadece ondalık noktasını koru)
                if (cleanPriceText.Contains("."))
                {
                    var parts = cleanPriceText.Split('.');
                    if (parts.Length > 2) // Birden fazla nokta varsa binlik ayırıcı
                    {
                        cleanPriceText = string.Join("", parts[0..^1]) + "." + parts[^1];
                    }
                }

                logger.LogInformation("TL Fiyat temizlendi: '{CleanPriceText}'", cleanPriceText);

                if (decimal.TryParse(cleanPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal price))
                {
                    product.Price = price;
                    logger.LogInformation("TL Fiyat başarıyla parse edildi: {Price}", price);
                }
                else
                {
                    logger.LogWarning("TL Fiyat parse edilemedi: '{CleanPriceText}'", cleanPriceText);
                }
                // Fiyat USD (6. hücre) - Metin formatında parsing
                var usdPriceText = cells[5].InnerText.Trim();
                logger.LogInformation("USD Fiyat parsing: '{PriceText}'", usdPriceText);

                // USD fiyat formatını temizle
                var cleanUsdPriceText = usdPriceText.Replace("USD", "").Replace("$", "").Replace(" ", "").Trim();

                // Türkçe ondalık ayırıcı (virgül) -> nokta dönüşümü
                cleanUsdPriceText = cleanUsdPriceText.Replace(",", ".");

                // Binlik ayırıcı noktaları kaldır
                if (cleanUsdPriceText.Contains("."))
                {
                    var parts = cleanUsdPriceText.Split('.');
                    if (parts.Length > 2)
                    {
                        cleanUsdPriceText = string.Join("", parts[0..^1]) + "." + parts[^1];
                    }
                }

                logger.LogInformation("USD Fiyat temizlendi: '{CleanPriceText}'", cleanUsdPriceText);

                if (decimal.TryParse(cleanUsdPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal usdPrice))
                {
                    product.UsdPrice = usdPrice;
                    logger.LogInformation("USD Fiyat başarıyla parse edildi: {Price}", usdPrice);
                }
                else
                {
                    logger.LogWarning("USD Fiyat parse edilemedi: '{CleanPriceText}'", cleanUsdPriceText);
                }

                // Fiyat EUR (7. hücre) - Metin formatında parsing
                var euroPriceText = cells[6].InnerText.Trim();
                logger.LogInformation("EUR Fiyat parsing: '{PriceText}'", euroPriceText);

                // EUR fiyat formatını temizle
                var cleanEuroPriceText = euroPriceText.Replace("EURO", "").Replace("€", "").Replace(" ", "").Trim();

                // Türkçe ondalık ayırıcı (virgül) -> nokta dönüşümü
                cleanEuroPriceText = cleanEuroPriceText.Replace(",", ".");

                // Binlik ayırıcı noktaları kaldır
                if (cleanEuroPriceText.Contains("."))
                {
                    var parts = cleanEuroPriceText.Split('.');
                    if (parts.Length > 2)
                    {
                        cleanEuroPriceText = string.Join("", parts[0..^1]) + "." + parts[^1];
                    }
                }

                logger.LogInformation("EUR Fiyat temizlendi: '{CleanPriceText}'", cleanEuroPriceText);

                if (decimal.TryParse(cleanEuroPriceText, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal euroPrice))
                {
                    product.EuroPrice = euroPrice;
                    logger.LogInformation("EUR Fiyat başarıyla parse edildi: {Price}", euroPrice);
                }
                else
                {
                    logger.LogWarning("EUR Fiyat parse edilemedi: '{CleanPriceText}'", cleanEuroPriceText);
                }

                products.Add(product);
                logger.LogInformation("Ürün eklendi: {ProductName} (ID: {ProductId})", product.Name, product.Id);
            }

            logger.LogInformation("HTML'den {Count} ürün parse edildi", products.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTML parsing hatası");
        }

        return products;
    }

    private class ProductInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? CategorySlug { get; set; }
        public string? SubCategorySlug { get; set; }
        public string? Dimensions { get; set; }
        public decimal Price { get; set; }
        public decimal UsdPrice { get; set; }
        public decimal EuroPrice { get; set; }
        public string? ImageBase64 { get; set; }
    }

    private Task<byte[]> GenerateSimplePdfAsync(string htmlContent, string fileName)
    {
        try
        {
            logger.LogInformation("Basit PDF oluşturma deneniyor: {FileName}", fileName);

            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 20, 20, 20, 20);
            PdfWriter.GetInstance(document, memoryStream);

            document.Open();

            // Header - Logo ve iletişim bilgileri
            AddHeaderToDocument(document);

            // HTML içeriğinden ürün bilgilerini parse et
            var products = ParseProductsFromHtml(htmlContent);

            if (products.Count == 0)
            {
                // Ürün bulunamazsa basit mesaj - Türkçe font kullan
                var noProductsFont = GetTurkishFont(12, Font.NORMAL, BaseColor.GRAY);
                var noProducts = new Paragraph("Ürün bulunamadı veya veri parse edilemedi.", noProductsFont)
                {
                    Alignment = Element.ALIGN_CENTER
                };
                document.Add(noProducts);
            }
            else
            {
                // Basit liste formatında ürünleri göster - Türkçe font kullan
                var productFont = GetTurkishFont(10);
                var priceFont = GetTurkishFont(10, Font.BOLD, BaseColor.DARK_GRAY);

                foreach (var product in products)
                {
                    // Ürün adı - tıklanabilir link ile
                    var productUrl = urlService.GetProductUrl(product.CategorySlug ?? "urunler", product.SubCategorySlug ?? "tum-urunler", product.Slug);
                    var productLink = new Chunk($"• {product.Name}", GetTurkishFont(10, Font.UNDERLINE, BaseColor.BLUE));
                    productLink.SetAction(new PdfAction(productUrl));
                    var productName = new Paragraph();
                    productName.Add(productLink);
                    document.Add(productName);

                    // Ürün detayları
                    var details = new Paragraph($"  Kod: U-{product.Id} | Ölçü: {product.Dimensions ?? "Belirtilmemiş"}", productFont);
                    document.Add(details);

                    // Fiyatlar
                    var prices = new Paragraph($"  {product.Price:N2} | USD {product.UsdPrice:N2} | EURO {product.EuroPrice:N2}", priceFont);
                    document.Add(prices);

                    document.Add(new Paragraph(" ")); // Boş satır
                }
            }

            document.Close();

            var pdfBytes = memoryStream.ToArray();
            logger.LogInformation("Basit PDF başarıyla oluşturuldu. Boyut: {Size} bytes", pdfBytes.Length);
            return Task.FromResult(pdfBytes);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Basit PDF oluşturma hatası");
            throw new Exception($"PDF oluşturulurken hata oluştu: {ex.Message}", ex);
        }
    }
}
