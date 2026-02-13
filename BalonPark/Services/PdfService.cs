using HtmlAgilityPack;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.Globalization;
using BalonPark.Data;
using BalonPark.Models;

namespace BalonPark.Services;

public class PdfService(
    ILogger<PdfService> logger, 
    IUrlService urlService, 
    SettingsRepository settingsRepository,
    IWebHostEnvironment environment,
    IHttpClientFactory httpClientFactory)
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

    /// <summary>
    /// Ürün detay sayfasındaki ilgili ürünler listesini PDF kataloğu olarak üretir (public sayfa export).
    /// </summary>
    public async Task<byte[]> GenerateRelatedProductsCatalogPdfAsync(IReadOnlyList<ProductWithImage> relatedProducts, string currentProductName)
    {
        return await Task.Run(async () =>
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, CatalogMarginH, CatalogMarginH, CatalogMarginBottom, CatalogMarginV);
            var writer = PdfWriter.GetInstance(document, memoryStream);
            writer.PageEvent = new PdfBackgroundColorEvent(PdfBackgroundColor);
            document.Open();

            AddHeaderToDocument(document);

            var titleFont = GetTurkishFont(14, Font.BOLD, new BaseColor(28, 28, 28));
            document.Add(new Paragraph("İlgili Ürünler", titleFont) { SpacingAfter = 2 });
            document.Add(new Paragraph(currentProductName, GetTurkishFont(10, Font.NORMAL, BaseColor.GRAY)) { SpacingAfter = CatalogSectionSpacing });

            if (relatedProducts.Count == 0)
            {
                document.Add(new Paragraph("Bu ürün için ilgili ürün listesi boş.", GetTurkishFont(10)));
                document.Close();
                return memoryStream.ToArray();
            }

            var table = new PdfPTable(7)
            {
                WidthPercentage = 100,
                HeaderRows = 1,
                SpacingBefore = 4,
                SpacingAfter = CatalogSectionSpacing,
                HorizontalAlignment = Element.ALIGN_LEFT
            };
            table.SetWidths([0.9f, 0.9f, 2.6f, 1.2f, 1f, 1f, 1f]);

            var headerFont = GetTurkishFont(9, Font.BOLD, BaseColor.WHITE);
            var headers = new[] { "Resim", "Kod", "Ürün Adı", "Kategori", "Fiyat (TL)", "Fiyat (USD)", "Fiyat (EUR)" };
            foreach (var h in headers)
            {
                var cell = new PdfPCell(new Phrase(h, headerFont))
                {
                    BackgroundColor = CatalogHeaderBg,
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = CatalogHeaderPadding,
                    BorderColor = CatalogBorder,
                    BorderWidth = 0.5f
                };
                table.AddCell(cell);
            }

            var cellFont = GetTurkishFont(9);
            var webRoot = environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var rowIndex = 0;

            foreach (var item in relatedProducts)
            {
                var p = item.Product;
                var rowBg = (rowIndex % 2 == 1) ? CatalogZebraRow : BaseColor.WHITE;

                // Resim
                var imgCell = new PdfPCell
                {
                    HorizontalAlignment = Element.ALIGN_CENTER,
                    VerticalAlignment = Element.ALIGN_MIDDLE,
                    Padding = CatalogTablePadding,
                    FixedHeight = ThumbnailCellHeightPt,
                    BackgroundColor = rowBg,
                    BorderColor = CatalogBorder,
                    BorderWidth = 0.5f
                };
                if (item.MainImage != null && !string.IsNullOrEmpty(item.MainImage.ThumbnailPath))
                {
                    byte[]? rawBytes = null;
                    var thumbPath = Path.Combine(webRoot, item.MainImage.ThumbnailPath.TrimStart('/', '\\'));
                    if (File.Exists(thumbPath))
                        rawBytes = await File.ReadAllBytesAsync(thumbPath).ConfigureAwait(false);
                    if (rawBytes == null || rawBytes.Length == 0)
                        rawBytes = await FetchImageBytesFromUrlAsync(urlService.GetImageUrl(item.MainImage.ThumbnailPath)).ConfigureAwait(false);
                    var added = false;
                    if (rawBytes != null && rawBytes.Length > 0)
                    {
                        var squareBytes = CropImageToCenterSquare(rawBytes) ?? rawBytes;
                        try
                        {
                            var img = iTextSharp.text.Image.GetInstance(squareBytes);
                            AddScaledImageToCell(imgCell, img, ThumbnailImageSizePt, ThumbnailImageSizePt, fixedCellHeightPt: null);
                            added = true;
                        }
                        catch { /* placeholder below */ }
                    }
                    if (!added) AddPlaceholderImage(imgCell);
                }
                else { AddPlaceholderImage(imgCell); }
                table.AddCell(imgCell);

                var productUrl = urlService.GetProductUrl(p.CategorySlug ?? "", p.SubCategorySlug ?? "", p.Slug);
                var dataCellStyle = new Action<PdfPCell>(c =>
                {
                    c.VerticalAlignment = Element.ALIGN_MIDDLE;
                    c.Padding = CatalogTablePadding;
                    c.BackgroundColor = rowBg;
                    c.BorderColor = CatalogBorder;
                    c.BorderWidth = 0.5f;
                });

                var codePhrase = new Phrase();
                var codeChunk = new Chunk($"U-{p.Id}", GetTurkishFont(9, Font.UNDERLINE, BaseColor.BLUE));
                codeChunk.SetAction(new PdfAction(productUrl));
                codePhrase.Add(codeChunk);
                var codeCell = new PdfPCell(codePhrase);
                dataCellStyle(codeCell);
                codeCell.HorizontalAlignment = Element.ALIGN_CENTER;
                table.AddCell(codeCell);

                var namePhrase = new Phrase();
                var nameChunk = new Chunk(p.Name ?? "", GetTurkishFont(9, Font.UNDERLINE, BaseColor.BLUE));
                nameChunk.SetAction(new PdfAction(productUrl));
                namePhrase.Add(nameChunk);
                var nameCell = new PdfPCell(namePhrase);
                dataCellStyle(nameCell);
                table.AddCell(nameCell);

                var catCell = new PdfPCell(new Phrase(p.CategoryName ?? "-", cellFont));
                dataCellStyle(catCell);
                table.AddCell(catCell);

                var priceCellTl = new PdfPCell(new Phrase(p.Price.ToString("N2", CultureInfo.InvariantCulture), cellFont));
                dataCellStyle(priceCellTl);
                priceCellTl.HorizontalAlignment = Element.ALIGN_RIGHT;
                table.AddCell(priceCellTl);
                var priceCellUsd = new PdfPCell(new Phrase(p.UsdPrice.ToString("N2", CultureInfo.InvariantCulture), cellFont));
                dataCellStyle(priceCellUsd);
                priceCellUsd.HorizontalAlignment = Element.ALIGN_RIGHT;
                table.AddCell(priceCellUsd);
                var priceCellEur = new PdfPCell(new Phrase(p.EuroPrice.ToString("N2", CultureInfo.InvariantCulture), cellFont));
                dataCellStyle(priceCellEur);
                priceCellEur.HorizontalAlignment = Element.ALIGN_RIGHT;
                table.AddCell(priceCellEur);
                rowIndex++;
            }

            document.Add(table);
            document.Close();
            return memoryStream.ToArray();
        });
    }

    /// <summary>
    /// Ürün detay sayfasındaki ürünün tamamını PDF olarak üretir (tüm detaylar + tüm resimler).
    /// </summary>
    public async Task<byte[]> GenerateProductDetailPdfAsync(Product product, ProductImage? mainImage, IReadOnlyList<ProductImage> allImages)
    {
        return await Task.Run(async () =>
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, CompactMarginH, CompactMarginH, CompactMarginBottom, CompactMarginV);
            var writer = PdfWriter.GetInstance(document, memoryStream);
            writer.PageEvent = new PdfBackgroundColorEvent(PdfBackgroundColor);
            document.Open();

            AddHeaderToDocument(document);

            var webRoot = environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var titleFont = GetTurkishFont(16, Font.BOLD, new BaseColor(28, 28, 28));
            var sectionFont = GetTurkishFont(10, Font.BOLD, new BaseColor(62, 62, 130));
            var labelFont = GetTurkishFont(8, Font.BOLD, new BaseColor(60, 60, 60));
            var cellFont = GetTurkishFont(8);
            var productUrl = urlService.GetProductUrl(product.CategorySlug ?? "", product.SubCategorySlug ?? "", product.Slug);

            // —— Başlık (büyük) + altında tablo ile boşluk ——
            document.Add(new Paragraph(product.Name ?? "Ürün", titleFont) { SpacingAfter = 16 });
            var pageContentWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
            var topTable = new PdfPTable(2)
            {
                WidthPercentage = 100,
                SpacingAfter = CompactSectionSpacing,
                HorizontalAlignment = Element.ALIGN_LEFT,
                TotalWidth = pageContentWidth,
                LockedWidth = true
            };
            topTable.SetWidths([0.48f, 0.52f]);

            var imgCell = new PdfPCell
            {
                Padding = 2,
                Border = iTextSharp.text.Rectangle.BOX,
                BorderColor = CatalogBorder,
                BorderWidth = 0.5f,
                FixedHeight = MainImageCellHeightPt,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_CENTER,
                BackgroundColor = BaseColor.WHITE
            };
            string? imagePath = ResolveImagePath(webRoot, mainImage?.LargePath, mainImage?.ThumbnailPath);
            var mainImageAdded = false;
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                try
                {
                    var img = iTextSharp.text.Image.GetInstance(imagePath);
                    AddScaledImageToCell(imgCell, img, MainImageMaxWidthPt, MainImageMaxHeightPt, fixedCellHeightPt: null);
                    mainImageAdded = true;
                }
                catch { /* fallback to URL or placeholder */ }
            }
            if (!mainImageAdded)
            {
                var imageUrl = urlService.GetImageUrl(mainImage?.LargePath ?? mainImage?.ThumbnailPath ?? "");
                var imageBytes = await FetchImageBytesFromUrlAsync(imageUrl).ConfigureAwait(false);
                if (imageBytes != null && imageBytes.Length > 0)
                {
                    try
                    {
                        var img = iTextSharp.text.Image.GetInstance(imageBytes);
                        AddScaledImageToCell(imgCell, img, MainImageMaxWidthPt, MainImageMaxHeightPt, fixedCellHeightPt: null);
                        mainImageAdded = true;
                    }
                    catch { /* placeholder below */ }
                }
            }
            if (!mainImageAdded) AddPlaceholderImage(imgCell);
            topTable.AddCell(imgCell);

            var infoCell = new PdfPCell
            {
                VerticalAlignment = Element.ALIGN_TOP,
                Padding = 6,
                Border = iTextSharp.text.Rectangle.BOX,
                BorderColor = CatalogBorder,
                BorderWidth = 0.5f,
                BackgroundColor = CatalogInfoBg
            };
            var infoTable = new PdfPTable(2)
            {
                WidthPercentage = 100,
                HorizontalAlignment = Element.ALIGN_LEFT,
                SpacingBefore = 0,
                SpacingAfter = 0
            };
            infoTable.SetWidths([0.32f, 0.68f]);
            infoTable.DefaultCell.Border = iTextSharp.text.Rectangle.NO_BORDER;
            infoTable.DefaultCell.Padding = 2;
            infoTable.DefaultCell.PaddingTop = 3;
            infoTable.DefaultCell.PaddingBottom = 3;
            AddInfoRow(infoTable, "Ürün Kodu", $"U-{product.Id}", labelFont, cellFont, true, productUrl);
            AddInfoRow(infoTable, "Kategori", $"{product.CategoryName ?? "-"} / {product.SubCategoryName ?? "-"}", labelFont, cellFont, false, null);
            var priceFont = GetTurkishFont(8, Font.BOLD, BaseColor.DARK_GRAY);
            AddInfoRow(infoTable, "Fiyatlar", $"₺ {product.Price:N2}  ·  $ {product.UsdPrice:N2}  ·  € {product.EuroPrice:N2}", labelFont, priceFont, false, null);
            AddInfoRow(infoTable, "Stok", product.Stock > 0 ? product.Stock.ToString() : "Stok yok", labelFont, cellFont, false, null);
            var badges = new List<string>();
            if (product.IsDiscounted) badges.Add("İndirimli");
            if (product.IsPopular) badges.Add("Popüler");
            if (product.IsProjectSpecial) badges.Add("Projeye özel");
            if (badges.Count > 0)
                AddInfoRow(infoTable, "Özellikler", string.Join(", ", badges), labelFont, cellFont, false, null);
            infoCell.AddElement(infoTable);
            topTable.AddCell(infoCell);
            document.Add(topTable);

            if (!string.IsNullOrEmpty(product.Summary))
            {
                AddSectionTitleCompact(document, "Özet", sectionFont);
                document.Add(new Paragraph(product.Name ?? "Ürün", titleFont) { SpacingAfter = 4 });
                document.Add(new Paragraph(product.Summary, cellFont) { SpacingAfter = CompactSectionSpacing });
            }
            if (!string.IsNullOrEmpty(product.Description))
            {
                AddSectionTitleCompact(document, "Açıklama", sectionFont);
                var plainDesc = StripHtml(product.Description);
                if (plainDesc.Length > 2500) plainDesc = plainDesc[..2500] + "…";
                document.Add(new Paragraph(plainDesc, cellFont) { SpacingAfter = CompactSectionSpacing });
            }
            if (!string.IsNullOrEmpty(product.TechnicalDescription))
            {
                AddSectionTitleCompact(document, "Teknik Açıklama", sectionFont);
                var plainTech = StripHtml(product.TechnicalDescription);
                if (plainTech.Length > 2500) plainTech = plainTech[..2500] + "…";
                document.Add(new Paragraph(plainTech, cellFont) { SpacingAfter = CompactSectionSpacing });
            }

            // —— Sayfa 2: Ürün Görselleri — sayfa genişliği kadar, kenarda boşluk yok, aralarında margin ——
            if (allImages.Count > 0)
            {
                document.NewPage();
                AddHeaderToDocument(document);
                AddSectionTitleCompact(document, "Ürün Görselleri", sectionFont);
                const int cols = 3;
                var galleryPageWidth = document.PageSize.Width - document.LeftMargin - document.RightMargin;
                var margin = GalleryCellMarginPt;
                var cellTotal = galleryPageWidth / cols;
                var imgSize = (float)(cellTotal - 2 * margin);
                if (imgSize < 80) imgSize = 80;
                var cellHeight = (float)cellTotal;
                var imgTable = new PdfPTable(cols)
                {
                    TotalWidth = galleryPageWidth,
                    LockedWidth = true,
                    HorizontalAlignment = Element.ALIGN_LEFT,
                    SpacingAfter = CompactSectionSpacing
                };
                imgTable.SetWidths([1f, 1f, 1f]);
                foreach (var pi in allImages.Take(9))
                {
                    var c = new PdfPCell
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = margin,
                        FixedHeight = cellHeight,
                        Border = iTextSharp.text.Rectangle.BOX,
                        BorderColor = CatalogBorder,
                        BorderWidth = 0.5f,
                        BackgroundColor = BaseColor.WHITE
                    };
                    var added = false;
                    byte[]? rawBytes = null;
                    var path = ResolveImagePath(webRoot, pi.LargePath, pi.ThumbnailPath);
                    if (!string.IsNullOrEmpty(path) && File.Exists(path))
                        rawBytes = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
                    if (rawBytes == null || rawBytes.Length == 0)
                    {
                        var imageUrl = urlService.GetImageUrl(pi.LargePath ?? pi.ThumbnailPath ?? "");
                        rawBytes = await FetchImageBytesFromUrlAsync(imageUrl).ConfigureAwait(false);
                    }
                    if (rawBytes != null && rawBytes.Length > 0)
                    {
                        var squareBytes = CropImageToCenterSquare(rawBytes) ?? rawBytes;
                        try
                        {
                            var im = iTextSharp.text.Image.GetInstance(squareBytes);
                            AddScaledImageToCell(c, im, (float)imgSize, (float)imgSize, fixedCellHeightPt: null);
                            added = true;
                        }
                        catch { /* placeholder below */ }
                    }
                    if (!added) AddPlaceholderImage(c);
                    imgTable.AddCell(c);
                }
                while (imgTable.Size % cols != 0)
                    imgTable.AddCell(new PdfPCell(new Phrase(" "))
                    {
                        FixedHeight = cellHeight,
                        Padding = margin,
                        Border = iTextSharp.text.Rectangle.BOX,
                        BorderColor = CatalogBorder,
                        BorderWidth = 0.5f,
                        BackgroundColor = CatalogZebraRow
                    });
                document.Add(imgTable);
            }

            // —— Sayfa 3: Teknik Bilgiler (ayrı sayfa) ——
            var hasInflated = !string.IsNullOrEmpty(product.InflatedLength) || !string.IsNullOrEmpty(product.InflatedWidth) || !string.IsNullOrEmpty(product.InflatedHeight) || product.UserCount != null || product.InflatedWeightKg != null;
            var hasAssembly = product.AssemblyTime != null || product.RequiredPersonCount != null || !string.IsNullOrEmpty(product.FanDescription) || product.FanWeightKg != null;
            var hasPackaged = !string.IsNullOrEmpty(product.PackagedLength) || !string.IsNullOrEmpty(product.PackagedDepth) || product.PackagedWeightKg != null || product.PackagePalletCount != null;
            var hasGeneral = product.HasCertificate || !string.IsNullOrEmpty(product.WarrantyDescription) || !string.IsNullOrEmpty(product.AfterSalesService)
                || product.IsFireResistant || product.MaterialWeightGrm2 != null || !string.IsNullOrEmpty(product.MaterialWeight);
            if (hasInflated || hasAssembly || hasPackaged || hasGeneral)
            {
                document.NewPage();
                AddHeaderToDocument(document);
                AddSectionTitleCompact(document, "Teknik Bilgiler", sectionFont);
                var specTable = new PdfPTable(2)
                {
                    WidthPercentage = 100,
                    SpacingAfter = CompactSectionSpacing,
                    HorizontalAlignment = Element.ALIGN_LEFT
                };
                specTable.SetWidths([0.35f, 0.65f]);
                var pad = CompactTablePadding;
                var padTitle = CompactTablePadding + 2;
                if (hasInflated)
                {
                    AddSpecRow(specTable, "Şişmiş ürün", labelFont, cellFont, padTitle);
                    if (!string.IsNullOrEmpty(product.InflatedLength)) AddKeyValueRow(specTable, "Uzunluk", $"{product.InflatedLength} m", cellFont, null, pad);
                    if (!string.IsNullOrEmpty(product.InflatedWidth)) AddKeyValueRow(specTable, "Genişlik", $"{product.InflatedWidth} m", cellFont, null, pad);
                    if (!string.IsNullOrEmpty(product.InflatedHeight)) AddKeyValueRow(specTable, "Yükseklik", $"{product.InflatedHeight} m", cellFont, null, pad);
                    if (product.UserCount != null) AddKeyValueRow(specTable, "Kullanıcı sayısı", product.UserCount.Value.ToString(), cellFont, null, pad);
                    if (product.InflatedWeightKg != null) AddKeyValueRow(specTable, "Şişmiş ağırlık", $"{product.InflatedWeightKg} kg", cellFont, null, pad);
                }
                if (hasAssembly)
                {
                    AddSpecRow(specTable, "Montaj / demontaj", labelFont, cellFont, padTitle);
                    if (product.AssemblyTime != null) AddKeyValueRow(specTable, "Montaj süresi", $"{product.AssemblyTime} saat", cellFont, null, pad);
                    if (product.RequiredPersonCount != null) AddKeyValueRow(specTable, "Gerekli kişi", $"{product.RequiredPersonCount} kişi", cellFont, null, pad);
                    if (!string.IsNullOrEmpty(product.FanDescription)) AddKeyValueRow(specTable, "Fan", product.FanDescription, cellFont, null, pad);
                    if (product.FanWeightKg != null) AddKeyValueRow(specTable, "Fan ağırlığı", $"{product.FanWeightKg} kg", cellFont, null, pad);
                }
                if (hasPackaged)
                {
                    AddSpecRow(specTable, "Paketlenmiş", labelFont, cellFont, padTitle);
                    if (!string.IsNullOrEmpty(product.PackagedLength)) AddKeyValueRow(specTable, "Uzunluk", $"{product.PackagedLength} cm", cellFont, null, pad);
                    if (!string.IsNullOrEmpty(product.PackagedDepth)) AddKeyValueRow(specTable, "Derinlik", $"{product.PackagedDepth} cm", cellFont, null, pad);
                    if (product.PackagedWeightKg != null) AddKeyValueRow(specTable, "Ağırlık", $"{product.PackagedWeightKg} kg", cellFont, null, pad);
                    if (product.PackagePalletCount != null) AddKeyValueRow(specTable, "Palet sayısı", product.PackagePalletCount.Value.ToString(), cellFont, null, pad);
                }
                if (hasGeneral)
                {
                    AddSpecRow(specTable, "Genel", labelFont, cellFont, padTitle);
                    if (product.HasCertificate) AddKeyValueRow(specTable, "Sertifika", "Var", cellFont, null, pad);
                    if (!string.IsNullOrEmpty(product.WarrantyDescription)) AddKeyValueRow(specTable, "Garanti", product.WarrantyDescription, cellFont, null, pad);
                    if (!string.IsNullOrEmpty(product.AfterSalesService)) AddKeyValueRow(specTable, "Satış sonrası", product.AfterSalesService, cellFont, null, pad);
                    if (product.IsFireResistant) AddKeyValueRow(specTable, "Ateşe dayanıklı", "Evet", cellFont, null, pad);
                    if (product.MaterialWeightGrm2 != null) AddKeyValueRow(specTable, "Kumaş ağırlığı", $"{product.MaterialWeightGrm2} gr/m²", cellFont, null, pad);
                    else if (!string.IsNullOrEmpty(product.MaterialWeight)) AddKeyValueRow(specTable, "Kumaş ağırlığı", product.MaterialWeight, cellFont, null, pad);
                }
                document.Add(specTable);
            }

            var hasOther = product.DeliveryDaysMin != null || product.DeliveryDaysMax != null || !string.IsNullOrEmpty(product.DeliveryDays)
                || !string.IsNullOrEmpty(product.ColorOptions);
            if (hasOther)
            {
                AddSectionTitleCompact(document, "Diğer", sectionFont);
                if (product.DeliveryDaysMin != null || product.DeliveryDaysMax != null)
                    document.Add(new Paragraph($"Teslimat: {product.DeliveryDaysMin ?? 0}-{product.DeliveryDaysMax ?? 0} iş günü", cellFont) { SpacingAfter = 1 });
                else if (!string.IsNullOrEmpty(product.DeliveryDays))
                    document.Add(new Paragraph($"Teslimat: {product.DeliveryDays}", cellFont) { SpacingAfter = 1 });
                if (!string.IsNullOrEmpty(product.ColorOptions))
                    document.Add(new Paragraph($"Renk seçenekleri: {product.ColorOptions}", cellFont) { SpacingAfter = 1 });
            }
            document.Add(new Paragraph(" ", cellFont) { SpacingAfter = 4 });
            var linkChunk = new Chunk("Ürün sayfası: " + productUrl, GetTurkishFont(8, Font.UNDERLINE, BaseColor.BLUE));
            linkChunk.SetAction(new PdfAction(productUrl));
            document.Add(new Paragraph(linkChunk));

            document.Close();
            return memoryStream.ToArray();
        });
    }

    // —— Katalog tasarım sabitleri ——
    private const float CatalogMarginH = 36f;
    private const float CatalogMarginV = 36f;
    private const float CatalogMarginBottom = 42f;
    private const float CatalogSectionSpacing = 14f;
    // Ürün detay PDF: compact layout
    private const float CompactMarginH = 24f;
    private const float CompactMarginV = 24f;
    private const float CompactMarginBottom = 28f;
    private const float CompactSectionSpacing = 6f;
    private const float CompactTablePadding = 3f;
    private static readonly BaseColor CatalogHeaderBg = new(62, 62, 130);
    private static readonly BaseColor CatalogZebraRow = new(248, 248, 252);
    private static readonly BaseColor CatalogInfoBg = new(248, 249, 252);
    private static readonly BaseColor CatalogBorder = new(220, 222, 228);
    private const float CatalogTablePadding = 6f;
    private const float CatalogHeaderPadding = 8f;
    /// <summary>PDF sayfa arka plan rengi (#fbfbfb).</summary>
    private static readonly BaseColor PdfBackgroundColor = new(251, 251, 251);

    /// <summary>Ürün detay PDF: ana resim hücresi (compact).</summary>
    private const float MainImageCellHeightPt = 220f;
    /// <summary>Sol kolon — resim alanı geniş.</summary>
    private const float MainImageMaxWidthPt = 240f;
    private const float MainImageMaxHeightPt = 220f;
    /// <summary>Ürün Görselleri: resimler arası margin (pt).</summary>
    private const float GalleryCellMarginPt = 6f;
    /// <summary>Tablo satırları: thumbnail hücre sabit yükseklik (pt).</summary>
    private const float ThumbnailCellHeightPt = 70f;
    /// <summary>Tablo satırları: thumbnail resim max boyut (pt).</summary>
    private const float ThumbnailImageSizePt = 50f;

    /// <summary>Her sayfaya #fbfbfb arka plan çizer.</summary>
    private sealed class PdfBackgroundColorEvent : PdfPageEventHelper
    {
        private readonly BaseColor _color;

        public PdfBackgroundColorEvent(BaseColor color) => _color = color;

        public override void OnEndPage(PdfWriter writer, Document document)
        {
            var canvas = writer.DirectContentUnder;
            canvas.SetColorFill(_color);
            canvas.Rectangle(0, 0, document.PageSize.Width, document.PageSize.Height);
            canvas.Fill();
        }
    }

    private static string? ResolveImagePath(string webRoot, string? largePath, string? thumbPath)
    {
        if (!string.IsNullOrEmpty(largePath))
            return Path.Combine(webRoot, largePath.TrimStart('/', '\\'));
        if (!string.IsNullOrEmpty(thumbPath))
            return Path.Combine(webRoot, thumbPath.TrimStart('/', '\\'));
        return null;
    }

    /// <summary>
    /// Resmi merkezden kare kırpar; PDF'te kare görünüm için kullanılır.
    /// </summary>
    private static byte[]? CropImageToCenterSquare(byte[] imageBytes)
    {
        try
        {
            using var image = SixLabors.ImageSharp.Image.Load(imageBytes);
            int size = Math.Min(image.Width, image.Height);
            if (size <= 0) return null;
            var cropRect = new SixLabors.ImageSharp.Rectangle(
                (image.Width - size) / 2,
                (image.Height - size) / 2,
                size,
                size);
            image.Mutate(x => x.Crop(cropRect));
            using var ms = new MemoryStream();
            image.SaveAsJpeg(ms);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resmi hücreye ekler: sabit hücre yüksekliği, resim ScaleToFit ile ortalanır (iText best practice).
    /// </summary>
    private static void AddScaledImageToCell(PdfPCell cell, iTextSharp.text.Image image, float maxWidthPt, float maxHeightPt, float? fixedCellHeightPt = null)
    {
        image.ScaleToFit(maxWidthPt, maxHeightPt);
        image.Alignment = Element.ALIGN_CENTER;
        if (fixedCellHeightPt.HasValue)
        {
            cell.FixedHeight = fixedCellHeightPt.Value;
            cell.VerticalAlignment = Element.ALIGN_MIDDLE;
        }
        cell.HorizontalAlignment = Element.ALIGN_CENTER;
        cell.AddElement(image);
    }

    /// <summary>
    /// Resmi URL'den indirir. Hata durumunda null döner.
    /// </summary>
    private async Task<byte[]?> FetchImageBytesFromUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(url) || (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
            return null;
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(15);
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "BalonPark-Pdf/1.0");
            var bytes = await client.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
            return bytes is { Length: > 0 } ? bytes : null;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Resim URL'den indirilemedi: {Url}", url);
            return null;
        }
    }

    private static string StripHtml(string html)
    {
        try
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            return doc.DocumentNode.InnerText.Trim().Replace("&nbsp;", " ");
        }
        catch { return html; }
    }

    private static void AddKeyValue(PdfPCell cell, string label, string value, Font labelFont, Font valueFont, bool asLink, string? url)
    {
        cell.AddElement(new Paragraph(label + ":", labelFont));
        if (asLink && !string.IsNullOrEmpty(url))
        {
            var ch = new Chunk(value, valueFont);
            ch.SetAction(new PdfAction(url));
            var p = new Paragraph(); p.Add(ch); cell.AddElement(p);
        }
        else
            cell.AddElement(new Paragraph(value, valueFont));
    }

    /// <summary>Compact bilgi kutusu: satır arası minimum.</summary>
    private static void AddKeyValueCompact(PdfPCell cell, string label, string value, Font labelFont, Font valueFont, bool asLink, string? url)
    {
        cell.AddElement(new Paragraph(label + ":", labelFont) { SpacingAfter = 0 });
        if (asLink && !string.IsNullOrEmpty(url))
        {
            var ch = new Chunk(value, valueFont);
            ch.SetAction(new PdfAction(url));
            var p = new Paragraph(); p.Add(ch); p.SpacingAfter = 1; cell.AddElement(p);
        }
        else
            cell.AddElement(new Paragraph(value, valueFont) { SpacingAfter = 1 });
    }

    /// <summary>Bilgi tablosu satırı: etiket (sol) | değer (sol), hizalı.</summary>
    private static void AddInfoRow(PdfPTable table, string label, string value, Font labelFont, Font valueFont, bool asLink, string? url)
    {
        var labelCell = new PdfPCell(new Phrase(label + ":", labelFont))
        {
            Border = iTextSharp.text.Rectangle.NO_BORDER,
            Padding = 2,
            PaddingTop = 3,
            PaddingBottom = 3,
            VerticalAlignment = Element.ALIGN_TOP
        };
        table.AddCell(labelCell);
        PdfPCell valueCell;
        if (asLink && !string.IsNullOrEmpty(url))
        {
            var ch = new Chunk(value, valueFont);
            ch.SetAction(new PdfAction(url));
            valueCell = new PdfPCell(new Phrase(ch))
            {
                Border = iTextSharp.text.Rectangle.NO_BORDER,
                Padding = 2,
                PaddingTop = 3,
                PaddingBottom = 3,
                VerticalAlignment = Element.ALIGN_TOP
            };
        }
        else
        {
            valueCell = new PdfPCell(new Phrase(value, valueFont))
            {
                Border = iTextSharp.text.Rectangle.NO_BORDER,
                Padding = 2,
                PaddingTop = 3,
                PaddingBottom = 3,
                VerticalAlignment = Element.ALIGN_TOP
            };
        }
        table.AddCell(valueCell);
    }

    private static void AddSectionTitle(Document document, string title, Font font, float spacingAfter = 0f)
    {
        var p = new Paragraph(title, font) { SpacingAfter = spacingAfter > 0 ? spacingAfter : CatalogSectionSpacing };
        document.Add(p);
    }

    private static void AddSectionTitleCompact(Document document, string title, Font font)
    {
        document.Add(new Paragraph(title, font) { SpacingAfter = CompactSectionSpacing });
    }

    private static void AddSpecRow(PdfPTable table, string title, Font labelFont, Font cellFont)
    {
        AddSpecRow(table, title, labelFont, cellFont, CatalogTablePadding + 2);
    }

    private static void AddSpecRow(PdfPTable table, string title, Font labelFont, Font cellFont, float padding)
    {
        var c = new PdfPCell(new Phrase(title, labelFont))
        {
            Colspan = 2,
            BackgroundColor = CatalogInfoBg,
            Padding = padding,
            BorderColor = CatalogBorder,
            BorderWidth = 0.5f
        };
        table.AddCell(c);
    }

    private static void AddKeyValueRow(PdfPTable table, string key, string value, Font cellFont, BaseColor? rowBg = null)
    {
        AddKeyValueRow(table, key, value, cellFont, rowBg, CatalogTablePadding);
    }

    private static void AddKeyValueRow(PdfPTable table, string key, string value, Font cellFont, BaseColor? rowBg, float padding)
    {
        var keyCell = new PdfPCell(new Phrase(key, cellFont))
        {
            Padding = padding,
            BorderColor = CatalogBorder,
            BorderWidth = 0.5f
        };
        var valCell = new PdfPCell(new Phrase(value, cellFont))
        {
            Padding = padding,
            BorderColor = CatalogBorder,
            BorderWidth = 0.5f
        };
        if (rowBg != null) { keyCell.BackgroundColor = rowBg; valCell.BackgroundColor = rowBg; }
        table.AddCell(keyCell);
        table.AddCell(valCell);
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
            var companyName = settings?.CompanyName ?? "BALON PARK";
            
            // Header: dar (düşük) yükseklik, resim alanı geniş kalsın
            var headerTable = new PdfPTable(2)
            {
                WidthPercentage = 100,
                SpacingBefore = 0,
                SpacingAfter = 2
            };
            headerTable.SetWidths([0.4f, 0.6f]);

            var logoCell = new PdfPCell
            {
                Border = iTextSharp.text.Rectangle.NO_BORDER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_LEFT,
                Padding = 1
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
                    logo.ScaleAbsolute(150, 30);
                    logoCell.AddElement(logo);
                    logger.LogInformation("Logo başarıyla yüklendi: {Path}", logoPath);
                }
                else
                {
                    // Logo yoksa metin ekle
                    logger.LogWarning("Logo dosyası bulunamadı: {Path}", logoPath);
                    var logoTextFont = GetTurkishFont(11, Font.BOLD, BaseColor.DARK_GRAY);
                    logoCell.AddElement(new Paragraph(companyName, logoTextFont));
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Logo yüklenirken hata, metin kullanılıyor");
                var logoTextFont = GetTurkishFont(11, Font.BOLD, BaseColor.DARK_GRAY);
                logoCell.AddElement(new Paragraph(companyName, logoTextFont));
            }

            headerTable.AddCell(logoCell);

            var contactCell = new PdfPCell
            {
                Border = iTextSharp.text.Rectangle.NO_BORDER,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                HorizontalAlignment = Element.ALIGN_RIGHT,
                Padding = 1
            };

            // İletişim: küçük fontlarla dar header
            var contactTitleFont = GetTurkishFont(9, Font.BOLD, BaseColor.DARK_GRAY);
            var contactFont = GetTurkishFont(8, Font.NORMAL, BaseColor.DARK_GRAY);
            var contactSmallFont = GetTurkishFont(7, Font.NORMAL, BaseColor.GRAY);

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
            document.Add(new Paragraph(" ", GetTurkishFont(8)) { SpacingAfter = 2 });
            var line = new LineSeparator(0.5f, 100f, CatalogBorder, Element.ALIGN_CENTER, -1);
            document.Add(new Chunk(line));
            document.Add(new Paragraph(" ", GetTurkishFont(8)) { SpacingAfter = 2 });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Header eklenirken hata oluştu");
        }
    }


    private async Task<byte[]> GeneratePdfWithiTextSharpAsync(string htmlContent, string fileName)
    {
        try
        {
            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, CatalogMarginH, CatalogMarginH, CatalogMarginBottom, CatalogMarginV);
            var writer = PdfWriter.GetInstance(document, memoryStream);
            writer.PageEvent = new PdfBackgroundColorEvent(PdfBackgroundColor);
            document.Open();

            AddHeaderToDocument(document);

            var products = ParseProductsFromHtml(htmlContent);
            logger.LogInformation("Parse edilen ürün sayısı: {Count}", products.Count);

            if (products.Count == 0)
            {
                var emptyFont = GetTurkishFont(12, Font.NORMAL, BaseColor.GRAY);
                document.Add(new Paragraph("Ürün Kataloğu", GetTurkishFont(14, Font.BOLD, new BaseColor(28, 28, 28))) { SpacingAfter = 4 });
                document.Add(new Paragraph("Ürün bulunamadı.", emptyFont) { Alignment = Element.ALIGN_CENTER, SpacingAfter = 20 });
            }
            else
            {
                document.Add(new Paragraph("Ürün Kataloğu", GetTurkishFont(14, Font.BOLD, new BaseColor(28, 28, 28))) { SpacingAfter = 2 });
                document.Add(new Paragraph($"{products.Count} ürün listeleniyor.", GetTurkishFont(10, Font.NORMAL, BaseColor.GRAY)) { SpacingAfter = CatalogSectionSpacing });

                var table = new PdfPTable(7)
                {
                    WidthPercentage = 100,
                    HeaderRows = 1,
                    SpacingBefore = 4,
                    SpacingAfter = CatalogSectionSpacing,
                    HorizontalAlignment = Element.ALIGN_LEFT
                };
                table.SetWidths([0.9f, 0.9f, 2.6f, 1.2f, 1.2f, 1.2f, 1.2f]);

                var headerFont = GetTurkishFont(9, Font.BOLD, BaseColor.WHITE);
                var headers = new[] { "Resim", "Kod", "Ürün Adı", "Özet", "Fiyat (TL)", "Fiyat (USD)", "Fiyat (EURO)" };
                foreach (var h in headers)
                {
                    var headerCell = new PdfPCell(new Phrase(h, headerFont))
                    {
                        BackgroundColor = CatalogHeaderBg,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = CatalogHeaderPadding,
                        BorderColor = CatalogBorder,
                        BorderWidth = 0.5f
                    };
                    table.AddCell(headerCell);
                }

                var cellFont = GetTurkishFont(9);
                var priceFont = GetTurkishFont(9, Font.BOLD, BaseColor.DARK_GRAY);
                var htmlRowIndex = 0;

                foreach (var product in products)
                {
                    var rowBg = (htmlRowIndex % 2 == 1) ? CatalogZebraRow : BaseColor.WHITE;

                    var cell = new PdfPCell
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = CatalogTablePadding,
                        FixedHeight = ThumbnailCellHeightPt,
                        BackgroundColor = rowBg,
                        BorderColor = CatalogBorder,
                        BorderWidth = 0.5f
                    };

                    var imageAdded = false;
                    byte[]? rawBytes = null;
                    if (!string.IsNullOrEmpty(product.ImageBase64))
                        try { rawBytes = Convert.FromBase64String(product.ImageBase64); } catch { /* ignore */ }
                    if ((rawBytes == null || rawBytes.Length == 0) && !string.IsNullOrEmpty(product.ImageUrl))
                        rawBytes = await FetchImageBytesFromUrlAsync(product.ImageUrl).ConfigureAwait(false);
                    if (rawBytes != null && rawBytes.Length > 0)
                    {
                        var squareBytes = CropImageToCenterSquare(rawBytes) ?? rawBytes;
                        try
                        {
                            var image = iTextSharp.text.Image.GetInstance(squareBytes);
                            AddScaledImageToCell(cell, image, ThumbnailImageSizePt, ThumbnailImageSizePt, fixedCellHeightPt: null);
                            imageAdded = true;
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Resim PDF'e eklenirken hata: {ProductId}", product.Id);
                        }
                    }
                    if (!imageAdded)
                    {
                        AddPlaceholderImage(cell);
                    }

                    table.AddCell(cell);

                    var codeUrl = urlService.GetProductUrl(product.CategorySlug ?? "urunler", product.SubCategorySlug ?? "tum-urunler", product.Slug);
                    var codePhrase = new Phrase();
                    var codeLink = new Chunk($"U-{product.Id}", GetTurkishFont(9, Font.UNDERLINE, BaseColor.BLUE));
                    codeLink.SetAction(new PdfAction(codeUrl));
                    codePhrase.Add(codeLink);
                    var codeCell = new PdfPCell(codePhrase)
                    {
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = CatalogTablePadding,
                        BackgroundColor = rowBg,
                        BorderColor = CatalogBorder,
                        BorderWidth = 0.5f
                    };
                    table.AddCell(codeCell);

                    var productUrl = urlService.GetProductUrl(product.CategorySlug ?? "urunler", product.SubCategorySlug ?? "tum-urunler", product.Slug);
                    var productNamePhrase = new Phrase();
                    var productLink = new Chunk(product.Name, GetTurkishFont(9, Font.UNDERLINE, BaseColor.BLUE));
                    productLink.SetAction(new PdfAction(productUrl));
                    productNamePhrase.Add(productLink);
                    var nameCell = new PdfPCell(productNamePhrase)
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = CatalogTablePadding,
                        BackgroundColor = rowBg,
                        BorderColor = CatalogBorder,
                        BorderWidth = 0.5f
                    };
                    table.AddCell(nameCell);

                    var summaryCell = new PdfPCell(new Phrase(product.Summary ?? "-", cellFont))
                    {
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = CatalogTablePadding,
                        BackgroundColor = rowBg,
                        BorderColor = CatalogBorder,
                        BorderWidth = 0.5f
                    };
                    table.AddCell(summaryCell);

                    var priceCellTl = new PdfPCell(new Phrase($"{product.Price:N2}", priceFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = CatalogTablePadding,
                        BackgroundColor = rowBg,
                        BorderColor = CatalogBorder,
                        BorderWidth = 0.5f
                    };
                    table.AddCell(priceCellTl);
                    var priceCellUsd = new PdfPCell(new Phrase($"{product.UsdPrice:N2}", priceFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = CatalogTablePadding,
                        BackgroundColor = rowBg,
                        BorderColor = CatalogBorder,
                        BorderWidth = 0.5f
                    };
                    table.AddCell(priceCellUsd);
                    var priceCellEur = new PdfPCell(new Phrase($"{product.EuroPrice:N2}", priceFont))
                    {
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = CatalogTablePadding,
                        BackgroundColor = rowBg,
                        BorderColor = CatalogBorder,
                        BorderWidth = 0.5f
                    };
                    table.AddCell(priceCellEur);
                    htmlRowIndex++;
                }

                document.Add(table);
            }

            document.Close();

            var pdfBytes = memoryStream.ToArray();
            logger.LogInformation("PDF oluşturuldu. Boyut: {Size} bytes", pdfBytes.Length);
            return pdfBytes;
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
                    if (!string.IsNullOrEmpty(srcAttribute))
                    {
                        if (srcAttribute.StartsWith("data:image", StringComparison.OrdinalIgnoreCase) && srcAttribute.Contains("base64,"))
                        {
                            var base64Index = srcAttribute.IndexOf("base64,", StringComparison.Ordinal) + 7;
                            product.ImageBase64 = srcAttribute[base64Index..];
                        }
                        else if (srcAttribute.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || srcAttribute.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                        {
                            product.ImageUrl = srcAttribute;
                        }
                        else
                        {
                            // Relative path: tam URL'e çevir (site/ImageBaseUrl üzerinden)
                            product.ImageUrl = urlService.GetImageUrl(srcAttribute);
                        }
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

                // Özet (4. hücre)
                product.Summary = cells[3].InnerText.Trim();

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
        public string? Summary { get; set; }
        public decimal Price { get; set; }
        public decimal UsdPrice { get; set; }
        public decimal EuroPrice { get; set; }
        public string? ImageBase64 { get; set; }
        /// <summary>Resim base64 değilse URL'den çekilecek.</summary>
        public string? ImageUrl { get; set; }
    }

    private Task<byte[]> GenerateSimplePdfAsync(string htmlContent, string fileName)
    {
        try
        {
            logger.LogInformation("Basit PDF oluşturma deneniyor: {FileName}", fileName);

            using var memoryStream = new MemoryStream();
            var document = new Document(PageSize.A4, 20, 20, 20, 20);
            var writer = PdfWriter.GetInstance(document, memoryStream);
            writer.PageEvent = new PdfBackgroundColorEvent(PdfBackgroundColor);
            document.Open();

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
                    var details = new Paragraph($"  Kod: U-{product.Id} | Özet: {product.Summary ?? "-"}", productFont);
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
