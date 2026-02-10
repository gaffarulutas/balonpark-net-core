using Microsoft.AspNetCore.Mvc;
using BalonPark.Data;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Pages.Admin.Products;

public class IndexModel(
    ProductRepository productRepository,
    ProductImageRepository productImageRepository,
    CategoryRepository categoryRepository,
    SubCategoryRepository subCategoryRepository,
    PdfService pdfService,
    ExcelService excelService,
    IUrlService urlService,
    ICacheService cacheService)
    : BaseAdminPage
{
    public List<Product> Products { get; set; } = [];
    public Dictionary<int, ProductImage?> MainImages { get; set; } = new();
    public new List<Category> Categories { get; set; } = [];
    public List<SubCategory> SubCategories { get; set; } = [];
    
    [BindProperty(SupportsGet = true)]
    public List<int> SelectedCategoryIds { get; set; } = [];
    
    [BindProperty(SupportsGet = true)]
    public List<int> SelectedSubCategoryIds { get; set; } = [];
    
    [BindProperty(SupportsGet = true)]
    public string? ProductNameFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public string? ProductIdFilter { get; set; }
    
    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;
    
    public int PageSize { get; set; } = 20;
    public int TotalProducts { get; set; }
    public int TotalPages { get; set; }
    
    [TempData]
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        // Kategorileri ve alt kategorileri yükle
        Categories = (await categoryRepository.GetAllAsync()).ToList();
        SubCategories = (await subCategoryRepository.GetAllAsync()).ToList();
        
        // UrlService'i ViewData'ya ekle
        ViewData["UrlService"] = urlService;
        
        // Filtreleme uygula
        var allProducts = (await productRepository.GetAllAsync()).ToList();
        
        // Kategori filtresi
        if (SelectedCategoryIds.Any())
        {
            allProducts = allProducts.Where(p => SelectedCategoryIds.Contains(p.CategoryId)).ToList();
        }
        
        // Alt kategori filtresi
        if (SelectedSubCategoryIds.Any())
        {
            allProducts = allProducts.Where(p => SelectedSubCategoryIds.Contains(p.SubCategoryId)).ToList();
        }
        
        // Ürün adı filtresi
        if (!string.IsNullOrWhiteSpace(ProductNameFilter))
        {
            allProducts = allProducts.Where(p => 
                p.Name.Contains(ProductNameFilter, StringComparison.CurrentCultureIgnoreCase)).ToList();
        }
        
        // Ürün ID filtresi
        if (!string.IsNullOrWhiteSpace(ProductIdFilter))
        {
            if (int.TryParse(ProductIdFilter, out int productId))
            {
                allProducts = allProducts.Where(p => p.Id == productId).ToList();
            }
        }
        
        // Pagination
        TotalProducts = allProducts.Count;
        TotalPages = (int)Math.Ceiling(TotalProducts / (double)PageSize);
        
        // Sayfa numarası kontrolü
        if (PageNumber < 1) PageNumber = 1;
        if (PageNumber > TotalPages && TotalPages > 0) PageNumber = TotalPages;
        
        // Sayfaya göre ürünleri al
        Products = allProducts
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToList();
        
        foreach (var product in Products)
        {
            var mainImage = await productImageRepository.GetMainImageAsync(product.Id);
            MainImages[product.Id] = mainImage;
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        try
        {
            var product = await productRepository.GetByIdAsync(id);
            await productRepository.DeleteAsync(id);
            
            // Cache'i temizle
            if (cacheService != null)
            {
                await cacheService.InvalidateProductsAsync();
                await cacheService.InvalidateProductAsync(id);
                if (product != null)
                {
                    await cacheService.InvalidateProductBySlugAsync(product.Slug);
                }
            }
            
            SuccessMessage = "Ürün başarıyla silindi!";
        }
        catch
        {
            SuccessMessage = "Ürün silinirken hata oluştu!";
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetExportPdfAsync()
    {
        try
        {
            var allProducts = (await productRepository.GetAllAsync()).ToList();
            
            if (SelectedCategoryIds.Count != 0)
            {
                allProducts = allProducts.Where(p => SelectedCategoryIds.Contains(p.CategoryId)).ToList();
            }
            
            if (SelectedSubCategoryIds.Any())
            {
                allProducts = allProducts.Where(p => SelectedSubCategoryIds.Contains(p.SubCategoryId)).ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(ProductNameFilter))
            {
                allProducts = allProducts.Where(p => 
                    p.Name.Contains(ProductNameFilter, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(ProductIdFilter))
            {
                if (int.TryParse(ProductIdFilter, out int productId))
                {
                    allProducts = allProducts.Where(p => p.Id == productId).ToList();
                }
            }

            var productImages = new Dictionary<int, ProductImage?>();
            foreach (var product in allProducts)
            {
                try
                {
                    var mainImage = await productImageRepository.GetMainImageAsync(product.Id);
                    productImages[product.Id] = mainImage;
                }
                catch
                {
                    productImages[product.Id] = null;
                }
            }

            var htmlContent = GeneratePdfHtml(allProducts, productImages);
            var fileName = $"Urun_Fiyat_Katalogu_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            var pdfBytes = await pdfService.GeneratePdfFromHtmlAsync(htmlContent, fileName);

            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            
            // Return more detailed error information
            var errorMessage = $"PDF oluşturulurken hata oluştu: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $" İç Hata: {ex.InnerException.Message}";
            }
            
            return BadRequest(errorMessage);
        }
    }

    public async Task<IActionResult> OnGetExportExcelAsync()
    {
        try
        {
            var allProducts = (await productRepository.GetAllAsync()).ToList();
            
            if (SelectedCategoryIds.Any())
            {
                allProducts = allProducts.Where(p => SelectedCategoryIds.Contains(p.CategoryId)).ToList();
            }
            
            if (SelectedSubCategoryIds.Any())
            {
                allProducts = allProducts.Where(p => SelectedSubCategoryIds.Contains(p.SubCategoryId)).ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(ProductNameFilter))
            {
                allProducts = allProducts.Where(p => 
                    p.Name.Contains(ProductNameFilter, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }
            
            if (!string.IsNullOrWhiteSpace(ProductIdFilter))
            {
                if (int.TryParse(ProductIdFilter, out int productId))
                {
                    allProducts = allProducts.Where(p => p.Id == productId).ToList();
                }
            }

            var productsWithImages = new List<ProductWithImage>();
            foreach (var product in allProducts)
            {
                try
                {
                    var mainImage = await productImageRepository.GetMainImageAsync(product.Id);
                    productsWithImages.Add(new ProductWithImage
                    {
                        Product = product,
                        MainImage = mainImage
                    });
                }
                catch
                {
                    productsWithImages.Add(new ProductWithImage
                    {
                        Product = product,
                        MainImage = null
                    });
                }
            }

            var fileName = $"Urun_Fiyat_Katalogu_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            var excelBytes = excelService.GenerateExcelFromProducts(productsWithImages, fileName);

            return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            
            // Return more detailed error information
            var errorMessage = $"Excel oluşturulurken hata oluştu: {ex.Message}";
            if (ex.InnerException != null)
            {
                errorMessage += $" İç Hata: {ex.InnerException.Message}";
            }
            
            return BadRequest(errorMessage);
        }
    }

    private string GeneratePdfHtml(List<Product> products, Dictionary<int, ProductImage?> productImages)
    {
        var html = $@"
        <!DOCTYPE html>
        <html lang='tr'>
        <head>
            <meta charset='utf-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Ürün Fiyat Kataloğu</title>
            <style>
                {GetPdfStyles()}
            </style>
        </head>
        <body>
            <div class='header'>
                <h1>Ürün Fiyat Kataloğu</h1>
                <p class='export-date'>Oluşturulma Tarihi: {DateTime.Now:dd.MM.yyyy HH:mm}</p>
                <p class='product-count'>Toplam {products.Count} ürün</p>
            </div>
            
            <table class='product-table'>
                <thead>
                    <tr>
                        <th>Resim</th>
                        <th>Ürün Kodu</th>
                        <th>Ürün Adı</th>
                        <th>Ölçü</th>
                        <th>Fiyat (₺)</th>
                        <th>Fiyat ($)</th>
                        <th>Fiyat (€)</th>
                    </tr>
                </thead>
                <tbody>";

        foreach (var product in products)
        {
            string imageSrc = "";
            if (productImages.ContainsKey(product.Id) && productImages[product.Id] != null)
            {
                try
                {
                    var imagePath = productImages[product.Id]!.LargePath;
                    var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
                    
                    if (System.IO.File.Exists(fullPath))
                    {
                        var imageBytes = System.IO.File.ReadAllBytes(fullPath);
                        imageSrc = $"data:image/jpeg;base64,{Convert.ToBase64String(imageBytes)}";
                    }
                }
                catch
                {
                    // Continue without image
                }
            }

            html += $@"
                    <tr data-product-id='{product.Id}' data-product-slug='{product.Slug}' data-category-slug='{product.CategorySlug}' data-subcategory-slug='{product.SubCategorySlug}'>
                        <td class='image-cell'>
                            {(string.IsNullOrEmpty(imageSrc) ? "<div class='no-image'>Resim Yok</div>" : $"<img src='{imageSrc}' class='product-image' alt='{product.Name?.Replace("'", "&#39;")}'>")}
                        </td>
                        <td class='code-cell'>U-{product.Id}</td>
                        <td class='name-cell'>{product.Name?.Replace("'", "&#39;")}</td>
                        <td class='dimension-cell'>{(product.Dimensions ?? "Belirtilmemiş").Replace("'", "&#39;")}</td>
                        <td class='price-cell'>{product.Price:N2}</td>
                        <td class='price-cell'>{product.UsdPrice:N2}</td>
                        <td class='price-cell'>{product.EuroPrice:N2}</td>
                    </tr>";
        }

        html += @"
                </tbody>
            </table>
        </body>
        </html>";

        return html;
    }

    private string GetPdfStyles()
    {
        return @"
            * {
                box-sizing: border-box;
            }
            body {
                font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
                font-size: 12px;
                line-height: 1.4;
                color: #333;
                margin: 0;
                padding: 20px;
                background: white;
            }
            .header {
                text-align: center;
                margin-bottom: 30px;
                border-bottom: 3px solid #6262a6;
                padding-bottom: 20px;
                page-break-inside: avoid;
            }
            .header h1 {
                color: #6262a6;
                font-size: 28px;
                margin: 0 0 15px 0;
                font-weight: 700;
            }
            .export-date, .product-count {
                margin: 8px 0;
                color: #666;
                font-size: 12px;
                font-weight: 500;
            }
            .product-table {
                width: 100%;
                border-collapse: collapse;
                margin-top: 20px;
                page-break-inside: auto;
            }
            .product-table th {
                background-color: #6262a6;
                color: white;
                padding: 15px 10px;
                text-align: left;
                font-weight: bold;
                border: 1px solid #ddd;
                font-size: 13px;
            }
            .product-table td {
                padding: 12px 10px;
                border: 1px solid #ddd;
                vertical-align: middle;
                font-size: 11px;
            }
            .product-table tr:nth-child(even) {
                background-color: #f8f9fa;
            }
            .product-table tr:hover {
                background-color: #e9ecef;
            }
            .image-cell {
                width: 90px;
                text-align: center;
            }
            .product-image {
                max-width: 70px;
                max-height: 70px;
                object-fit: cover;
                border: 2px solid #ddd;
                border-radius: 4px;
            }
            .no-image {
                width: 70px;
                height: 70px;
                background-color: #f0f0f0;
                border: 2px solid #ddd;
                display: flex;
                align-items: center;
                justify-content: center;
                font-size: 10px;
                color: #999;
                border-radius: 4px;
            }
            .code-cell {
                font-family: 'Courier New', monospace;
                font-weight: bold;
                color: #6262a6;
                text-align: center;
                min-width: 90px;
                font-size: 12px;
            }
            .name-cell {
                font-weight: bold;
                min-width: 220px;
                font-size: 12px;
            }
            .dimension-cell {
                min-width: 120px;
                font-size: 11px;
            }
            .price-cell {
                text-align: right;
                min-width: 90px;
                font-weight: bold;
                color: #6262a6;
                font-size: 12px;
            }
            @media print {
                body {
                    padding: 15px;
                }
                .product-table {
                    font-size: 10px;
                }
                .product-table th,
                .product-table td {
                    padding: 8px 6px;
                }
            }";
    }
}

