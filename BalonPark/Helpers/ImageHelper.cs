using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace BalonPark.Helpers;

public static class ImageHelper
{
    /// <summary>Ürün resimleri için yüksek JPEG kalitesi (net görüntü, az sıkıştırma).</summary>
    private const int JpegQuality = 95;

    /// <summary>Dosya uzantısına göre yüksek kaliteli encoder döndürür.</summary>
    private static IImageEncoder GetEncoderForPath(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".png" => new PngEncoder { CompressionLevel = PngCompressionLevel.BestCompression },
            ".jpg" or ".jpeg" => new JpegEncoder { Quality = JpegQuality },
            _ => new JpegEncoder { Quality = JpegQuality }
        };
    }

    public static async Task<(string originalPath, string largePath, string thumbnailPath)> SaveProductImageAsync(
        IFormFile file, 
        string productFolderPath,
        string fileName)
    {
        var originalPath = Path.Combine(productFolderPath, $"original_{fileName}");
        var largePath = Path.Combine(productFolderPath, $"large_{fileName}");
        var thumbnailPath = Path.Combine(productFolderPath, $"thumb_{fileName}");

        // Orijinali olduğu gibi kaydet (kalite kaybı yok)
        using (var stream = new FileStream(originalPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var encoderLarge = GetEncoderForPath(largePath);
        var encoderThumb = GetEncoderForPath(thumbnailPath);

        // Large (1000px) – Lanczos3 ile keskin resize, yüksek kalite kayıt
        using (var stream = file.OpenReadStream())
        using (var image = await Image.LoadAsync(stream))
        {
            var largeWidth = 1000;
            var aspectRatio = (double)image.Height / image.Width;
            var largeHeight = (int)(largeWidth * aspectRatio);

            image.Mutate(x => x.Resize(largeWidth, largeHeight, KnownResamplers.Lanczos3));
            await image.SaveAsync(largePath, encoderLarge);
        }

        // Thumbnail (450px) – aynı kalite ayarları
        using (var stream = file.OpenReadStream())
        using (var image = await Image.LoadAsync(stream))
        {
            var thumbWidth = 450;
            var aspectRatio = (double)image.Height / image.Width;
            var thumbHeight = (int)(thumbWidth * aspectRatio);

            image.Mutate(x => x.Resize(thumbWidth, thumbHeight, KnownResamplers.Lanczos3));
            await image.SaveAsync(thumbnailPath, encoderThumb);
        }

        return (originalPath, largePath, thumbnailPath);
    }

    public static void DeleteProductImages(string originalPath, string largePath, string thumbnailPath)
    {
        if (File.Exists(originalPath))
            File.Delete(originalPath);
        
        if (File.Exists(largePath))
            File.Delete(largePath);
        
        if (File.Exists(thumbnailPath))
            File.Delete(thumbnailPath);
    }

    public static async Task<string> SaveBlogImageAsync(IFormFile file)
    {
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "blog");
        
        // Klasör yoksa oluştur
        if (!Directory.Exists(uploadsFolder))
        {
            Directory.CreateDirectory(uploadsFolder);
        }
        
        // Benzersiz dosya adı oluştur
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(uploadsFolder, fileName);
        
        // Resmi kaydet
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        
        // Web path döndür
        return $"/uploads/blog/{fileName}";
    }

    public static void DeleteBlogImage(string imagePath)
    {
        if (!string.IsNullOrEmpty(imagePath) && imagePath.StartsWith("/uploads/blog/"))
        {
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
    }

    public static async Task<string> SaveLogoAsync(IFormFile file)
    {
        // Sabit logo klasörü ve dosya adı
        var logoFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "assets", "images", "logo");
        
        // Klasör yoksa oluştur
        if (!Directory.Exists(logoFolder))
        {
            Directory.CreateDirectory(logoFolder);
        }
        
        // Sabit dosya adı (her zaman logo.png)
        var fileName = "logo.png";
        var filePath = Path.Combine(logoFolder, fileName);
        
        // Eski logoyu sil (varsa)
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        
        // Resmi kaydet
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }
        
        // Sabit web path döndür
        return "/assets/images/logo/logo.png";
    }

    public static void DeleteLogo(string imagePath)
    {
        if (!string.IsNullOrEmpty(imagePath) && imagePath.StartsWith("/uploads/logo/"))
        {
            var physicalPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imagePath.TrimStart('/'));
            if (File.Exists(physicalPath))
            {
                File.Delete(physicalPath);
            }
        }
    }

    /// <summary>
    /// Resme watermark (logo) ekler
    /// </summary>
    /// <param name="imagePath">Watermark eklenecek resmin dosya yolu</param>
    /// <param name="watermarkPath">Watermark olarak kullanılacak logo'nun dosya yolu</param>
    /// <param name="opacity">Watermark opaklığı (0.0 - 1.0 arası, 0.5 = %50 saydam)</param>
    /// <param name="scale">Watermark boyut ölçeği (0.1 = resmin %10'u)</param>
    /// <param name="position">Watermark pozisyonu (BottomRight, BottomLeft, TopRight, TopLeft, Center)</param>
    public static async Task AddWatermarkAsync(
        string imagePath, 
        string watermarkPath, 
        float opacity = 0.5f,
        float scale = 0.15f,
        WatermarkPosition position = WatermarkPosition.BottomRight)
    {
        Console.WriteLine($"AddWatermarkAsync - Image Path: {imagePath}");
        Console.WriteLine($"AddWatermarkAsync - Watermark Path: {watermarkPath}");
        Console.WriteLine($"AddWatermarkAsync - Image Exists: {File.Exists(imagePath)}");
        Console.WriteLine($"AddWatermarkAsync - Watermark Exists: {File.Exists(watermarkPath)}");
        
        if (!File.Exists(imagePath) || !File.Exists(watermarkPath))
        {
            Console.WriteLine("AddWatermarkAsync - One or both files do not exist, skipping watermark");
            return;
        }

        try
        {
            using var image = await Image.LoadAsync(imagePath);
            using var watermark = await Image.LoadAsync(watermarkPath);

            Console.WriteLine($"Watermark ekleniyor - Orijinal resim: {image.Width}x{image.Height}, Watermark: {watermark.Width}x{watermark.Height}");

            // Watermark boyutunu hesapla (resmin belirli bir oranı kadar)
            var watermarkWidth = (int)(image.Width * scale);
            var aspectRatio = (float)watermark.Height / watermark.Width;
            var watermarkHeight = (int)(watermarkWidth * aspectRatio);

            Console.WriteLine($"Watermark boyutu: {watermarkWidth}x{watermarkHeight}, Opacity: {opacity}");

            // Watermark'ı yeniden boyutlandır
            watermark.Mutate(x => x.Resize(watermarkWidth, watermarkHeight));

            // Watermark opaklığını ayarla
            watermark.Mutate(x => x.Opacity(opacity));

            // Pozisyonu hesapla (sol alt köşe)
            var point = CalculateWatermarkPosition(image.Width, image.Height, watermarkWidth, watermarkHeight, WatermarkPosition.BottomLeft);

            Console.WriteLine($"Watermark pozisyonu: {point.X}, {point.Y}");

            // Watermark'ı resme ekle
            image.Mutate(x => x.DrawImage(watermark, point, 1f));

            // Resmi yüksek kaliteli encoder ile kaydet (kalite kaybı olmasın)
            var encoder = GetEncoderForPath(imagePath);
            await image.SaveAsync(imagePath, encoder);

            Console.WriteLine("Watermark başarıyla eklendi!");
        }
        catch (Exception ex)
        {
            // Hata durumunda sessizce devam et (watermark eklenmezse de resim kaydedilmiş olur)
            Console.WriteLine($"Watermark ekleme hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// Watermark pozisyonunu hesaplar
    /// </summary>
    private static Point CalculateWatermarkPosition(
        int imageWidth, 
        int imageHeight, 
        int watermarkWidth, 
        int watermarkHeight, 
        WatermarkPosition position)
    {
        const int margin = 20; // Kenarlardan boşluk

        return position switch
        {
            WatermarkPosition.BottomRight => new Point(
                imageWidth - watermarkWidth - margin,
                imageHeight - watermarkHeight - margin
            ),
            WatermarkPosition.BottomLeft => new Point(
                margin,
                imageHeight - watermarkHeight - margin
            ),
            WatermarkPosition.TopRight => new Point(
                imageWidth - watermarkWidth - margin,
                margin
            ),
            WatermarkPosition.TopLeft => new Point(
                margin,
                margin
            ),
            WatermarkPosition.Center => new Point(
                (imageWidth - watermarkWidth) / 2,
                (imageHeight - watermarkHeight) / 2
            ),
            _ => new Point(imageWidth - watermarkWidth - margin, imageHeight - watermarkHeight - margin)
        };
    }

    /// <summary>
    /// Ürün resmini kaydet ve watermark ekle
    /// </summary>
    public static async Task<(string originalPath, string largePath, string thumbnailPath)> SaveProductImageWithWatermarkAsync(
        IFormFile file,
        string productFolderPath,
        string fileName,
        string? watermarkLogoPath = null,
        float watermarkOpacity = 0.5f,
        float watermarkScale = 0.15f)
    {
        // Önce normal şekilde kaydet
        var (originalPath, largePath, thumbnailPath) = await SaveProductImageAsync(file, productFolderPath, fileName);

        // Watermark logo yolu verilmişse ekle
        if (!string.IsNullOrEmpty(watermarkLogoPath))
        {
            var physicalWatermarkPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", watermarkLogoPath.TrimStart('/'));
            
            Console.WriteLine($"Watermark Logo Path: {watermarkLogoPath}");
            Console.WriteLine($"Physical Watermark Path: {physicalWatermarkPath}");
            Console.WriteLine($"Watermark File Exists: {File.Exists(physicalWatermarkPath)}");
            
            if (File.Exists(physicalWatermarkPath))
            {
                Console.WriteLine($"Adding watermark to large image: {largePath}");
                // Large resme watermark ekle
                await AddWatermarkAsync(largePath, physicalWatermarkPath, watermarkOpacity, watermarkScale);
                
                Console.WriteLine($"Adding watermark to thumbnail: {thumbnailPath}");
                // Thumbnail'e de ekle (biraz daha küçük)
                await AddWatermarkAsync(thumbnailPath, physicalWatermarkPath, watermarkOpacity, watermarkScale * 1.2f);
            }
            else
            {
                Console.WriteLine($"Watermark file not found at: {physicalWatermarkPath}");
            }
        }
        else
        {
            Console.WriteLine("Watermark logo path is empty or null");
        }

        return (originalPath, largePath, thumbnailPath);
    }

    /// <summary>
    /// Test watermark fonksiyonu - mevcut bir resme watermark ekler
    /// </summary>
    public static async Task TestWatermarkAsync(string imagePath, string watermarkPath)
    {
        Console.WriteLine($"Test Watermark - Image: {imagePath}");
        Console.WriteLine($"Test Watermark - Watermark: {watermarkPath}");
        
        if (!File.Exists(imagePath) || !File.Exists(watermarkPath))
        {
            Console.WriteLine("Test files not found");
            return;
        }
        
        await AddWatermarkAsync(imagePath, watermarkPath, 0.8f, 0.3f, WatermarkPosition.Center);
        Console.WriteLine("Test watermark completed");
    }

}

/// <summary>
/// Watermark pozisyon seçenekleri
/// </summary>
public enum WatermarkPosition
{
    BottomRight,
    BottomLeft,
    TopRight,
    TopLeft,
    Center
}

