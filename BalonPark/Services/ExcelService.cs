using ClosedXML.Excel;
using BalonPark.Models;

namespace BalonPark.Services;

public class ExcelService(ILogger<ExcelService> logger)
{

    public byte[] GenerateExcelFromProducts(IEnumerable<ProductWithImage> products, string fileName = "urunler.xlsx")
    {
        try
        {
            logger.LogInformation("Excel oluşturma işlemi başlatıldı: {FileName}", fileName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ürün Listesi");

            // Başlık satırı
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromArgb(98, 98, 166);
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Sütun başlıkları
            worksheet.Cell(1, 1).Value = "Resim";
            worksheet.Cell(1, 2).Value = "Kod";
            worksheet.Cell(1, 3).Value = "Ürün Adı";
            worksheet.Cell(1, 4).Value = "Kategori";
            worksheet.Cell(1, 5).Value = "Alt Kategori";
            worksheet.Cell(1, 6).Value = "Ölçü";
            worksheet.Cell(1, 7).Value = "Fiyat (₺)";
            worksheet.Cell(1, 8).Value = "Fiyat ($)";
            worksheet.Cell(1, 9).Value = "Fiyat (€)";

            // Sütun genişliklerini ayarla
            worksheet.Column(1).Width = 15; // Resim
            worksheet.Column(2).Width = 10; // Kod
            worksheet.Column(3).Width = 30; // Ürün Adı
            worksheet.Column(4).Width = 20; // Kategori
            worksheet.Column(5).Width = 20; // Alt Kategori
            worksheet.Column(6).Width = 15; // Ölçü
            worksheet.Column(7).Width = 12; // Fiyat TL
            worksheet.Column(8).Width = 12; // Fiyat USD
            worksheet.Column(9).Width = 12; // Fiyat EUR

            int row = 2;
            foreach (var productWithImage in products)
            {
                var product = productWithImage.Product;
                var mainImage = productWithImage.MainImage;

                // Resim hücresi - resim varsa ekle
                if (mainImage != null && !string.IsNullOrEmpty(mainImage.LargePath))
                {
                    try
                    {
                        // Resim dosyasını oku ve Excel'e ekle
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", mainImage.LargePath.TrimStart('/'));
                        if (File.Exists(imagePath))
                        {
                        var imageStream = new MemoryStream(File.ReadAllBytes(imagePath));
                        worksheet.AddPicture(imageStream)
                            .MoveTo(worksheet.Cell(row, 1))
                            .WithSize(50, 50);
                        }
                        else
                        {
                            worksheet.Cell(row, 1).Value = "-";
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Resim yüklenirken hata: {ProductId}", product.Id);
                        worksheet.Cell(row, 1).Value = "-";
                    }
                }
                else
                {
                    worksheet.Cell(row, 1).Value = "-";
                }

                // Diğer hücreler
                worksheet.Cell(row, 2).Value = $"U-{product.Id}";
                worksheet.Cell(row, 3).Value = product.Name;
                worksheet.Cell(row, 4).Value = product.CategoryName;
                worksheet.Cell(row, 5).Value = product.SubCategoryName;
                worksheet.Cell(row, 6).Value = product.Dimensions ?? "Belirtilmemiş";

                // Fiyatlar - sayısal format
                worksheet.Cell(row, 7).Value = product.Price;
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.DarkBlue;
                worksheet.Cell(row, 7).Style.Font.Bold = true;

                worksheet.Cell(row, 8).Value = product.UsdPrice;
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 8).Style.Font.FontColor = XLColor.DarkBlue;
                worksheet.Cell(row, 8).Style.Font.Bold = true;

                worksheet.Cell(row, 9).Value = product.EuroPrice;
                worksheet.Cell(row, 9).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 9).Style.Font.FontColor = XLColor.DarkBlue;
                worksheet.Cell(row, 9).Style.Font.Bold = true;

                // Satır yüksekliğini ayarla (resim için)
                worksheet.Row(row).Height = 60;

                row++;
            }

            // Tablo otomatik filtre ekle
            var range = worksheet.Range(1, 1, row - 1, 9);
            range.SetAutoFilter();

            // Kenarlık ekle
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Başlık satırı stilini koru
            var headerRange = worksheet.Range(1, 1, 1, 9);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(98, 98, 166);
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;


            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            var excelBytes = memoryStream.ToArray();

            logger.LogInformation("Excel başarıyla oluşturuldu. Boyut: {Size} bytes", excelBytes.Length);

            return excelBytes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Excel oluşturulurken hata oluştu: {FileName}", fileName);
            throw new Exception($"Excel oluşturulurken hata oluştu: {ex.Message}", ex);
        }
    }

    public byte[] GenerateExcelFromHtml(string htmlContent, string fileName = "urunler.xlsx")
    {
        try
        {
            logger.LogInformation("HTML'den Excel oluşturma işlemi başlatıldı: {FileName}", fileName);

            // HTML içeriğinden ürün bilgilerini parse et
            var products = ParseProductsFromHtml(htmlContent);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Ürün Listesi");

            // Başlık satırı
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.FromArgb(98, 98, 166);
            headerRow.Style.Font.FontColor = XLColor.White;
            headerRow.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            // Sütun başlıkları
            worksheet.Cell(1, 1).Value = "Resim";
            worksheet.Cell(1, 2).Value = "Kod";
            worksheet.Cell(1, 3).Value = "Ürün Adı";
            worksheet.Cell(1, 4).Value = "Ölçü";
            worksheet.Cell(1, 5).Value = "Fiyat (₺)";
            worksheet.Cell(1, 6).Value = "Fiyat ($)";
            worksheet.Cell(1, 7).Value = "Fiyat (€)";

            // Sütun genişliklerini ayarla
            worksheet.Column(1).Width = 15; // Resim
            worksheet.Column(2).Width = 10; // Kod
            worksheet.Column(3).Width = 30; // Ürün Adı
            worksheet.Column(4).Width = 15; // Ölçü
            worksheet.Column(5).Width = 12; // Fiyat TL
            worksheet.Column(6).Width = 12; // Fiyat USD
            worksheet.Column(7).Width = 12; // Fiyat EUR

            int row = 2;
            foreach (var product in products)
            {
                // Resim hücresi
                if (!string.IsNullOrEmpty(product.ImageBase64))
                {
                    try
                    {
                        var imageBytes = Convert.FromBase64String(product.ImageBase64);
                        var imageStream = new MemoryStream(imageBytes);
                        worksheet.AddPicture(imageStream)
                            .MoveTo(worksheet.Cell(row, 1))
                            .WithSize(50, 50);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Resim yüklenirken hata: {ProductId}", product.Id);
                        worksheet.Cell(row, 1).Value = "-";
                    }
                }
                else
                {
                    worksheet.Cell(row, 1).Value = "-";
                }

                // Diğer hücreler
                worksheet.Cell(row, 2).Value = $"U-{product.Id}";
                worksheet.Cell(row, 3).Value = product.Name;
                worksheet.Cell(row, 4).Value = product.Dimensions ?? "Belirtilmemiş";

                // Fiyatlar - sayısal format
                worksheet.Cell(row, 5).Value = product.Price;
                worksheet.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 5).Style.Font.FontColor = XLColor.DarkBlue;
                worksheet.Cell(row, 5).Style.Font.Bold = true;

                worksheet.Cell(row, 6).Value = product.UsdPrice;
                worksheet.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 6).Style.Font.FontColor = XLColor.DarkBlue;
                worksheet.Cell(row, 6).Style.Font.Bold = true;

                worksheet.Cell(row, 7).Value = product.EuroPrice;
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";
                worksheet.Cell(row, 7).Style.Font.FontColor = XLColor.DarkBlue;
                worksheet.Cell(row, 7).Style.Font.Bold = true;

                // Satır yüksekliğini ayarla (resim için)
                worksheet.Row(row).Height = 60;

                row++;
            }

            // Tablo otomatik filtre ekle
            var range = worksheet.Range(1, 1, row - 1, 7);
            range.SetAutoFilter();

            // Kenarlık ekle
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            // Başlık satırı stilini koru
            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(98, 98, 166);
            headerRange.Style.Font.FontColor = XLColor.White;
            headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            using var memoryStream = new MemoryStream();
            workbook.SaveAs(memoryStream);
            var excelBytes = memoryStream.ToArray();

            logger.LogInformation("HTML'den Excel başarıyla oluşturuldu. Boyut: {Size} bytes", excelBytes.Length);

            return excelBytes;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "HTML'den Excel oluşturulurken hata oluştu: {FileName}", fileName);
            throw new Exception($"Excel oluşturulurken hata oluştu: {ex.Message}", ex);
        }
    }

    private List<ProductInfo> ParseProductsFromHtml(string htmlContent)
    {
        var products = new List<ProductInfo>();
        
        try
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(htmlContent);
            
            var tableRows = doc.DocumentNode.SelectNodes("//table[@class='product-table']//tbody//tr");
            
            if (tableRows != null)
            {
                foreach (var row in tableRows)
                {
                    var cells = row.SelectNodes("td");
                    if (cells != null && cells.Count >= 7) // Resim, Kod, Ad, Ölçü, TL, USD, EUR
                    {
                        var product = new ProductInfo();
                        
                        // Resim hücresi (1. hücre) - img tag'inden src attribute'unu çıkar
                        var imageCell = cells[0];
                        var imgTag = imageCell.SelectSingleNode(".//img");
                        
                        if (imgTag != null)
                        {
                            var srcAttribute = imgTag.GetAttributeValue("src", "");
                            
                            if (srcAttribute.StartsWith("data:image") && srcAttribute.Contains("base64,"))
                            {
                                var base64Index = srcAttribute.IndexOf("base64,") + 7;
                                product.ImageBase64 = srcAttribute.Substring(base64Index);
                            }
                        }
                        
                        // Kod hücresi (2. hücre)
                        var codeCell = cells[1];
                        var codeText = codeCell.InnerText.Trim();
                        if (codeText.StartsWith("U-") && int.TryParse(codeText.Substring(2), out int id))
                        {
                            product.Id = id;
                        }
                        
                        // Ürün adı (3. hücre)
                        product.Name = cells[2].InnerText.Trim();
                        
                        // Ölçü (4. hücre)
                        product.Dimensions = cells[3].InnerText.Trim();
                        
                        // Fiyat TL (5. hücre)
                        var priceText = cells[4].InnerText.Trim();
                        if (decimal.TryParse(priceText.Replace("₺", "").Replace(",", "").Replace(".", ","), out decimal price))
                        {
                            product.Price = price;
                        }
                        
                        // Fiyat USD (6. hücre)
                        var usdPriceText = cells[5].InnerText.Trim();
                        if (decimal.TryParse(usdPriceText.Replace("$", "").Replace(",", "").Replace(".", ","), out decimal usdPrice))
                        {
                            product.UsdPrice = usdPrice;
                        }
                        
                        // Fiyat EUR (7. hücre)
                        var euroPriceText = cells[6].InnerText.Trim();
                        if (decimal.TryParse(euroPriceText.Replace("€", "").Replace(",", "").Replace(".", ","), out decimal euroPrice))
                        {
                            product.EuroPrice = euroPrice;
                        }
                        
                        products.Add(product);
                    }
                }
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
        public string? Dimensions { get; set; }
        public decimal Price { get; set; }
        public decimal UsdPrice { get; set; }
        public decimal EuroPrice { get; set; }
        public string? ImageBase64 { get; set; }
    }
}
