using Dapper;
using BalonPark.Models;
using BalonPark.Services;

namespace BalonPark.Data;

public class SettingsRepository(DapperContext context, ICacheService cacheService)
{

    /// <summary>
    /// Kullanıcı adı ve şifre ile admin kullanıcısını doğrula
    /// </summary>
    public async Task<Settings?> ValidateAdminAsync(string username, string password)
    {
        var query = @"
            SELECT * FROM Settings 
            WHERE UserName = @UserName AND Password = @Password";
        
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Settings>(
            query, 
            new { UserName = username, Password = password }
        );
    }

    /// <summary>
    /// ID'ye göre ayarları getir
    /// </summary>
    public async Task<Settings?> GetByIdAsync(int id)
    {
        var query = "SELECT * FROM Settings WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        return await connection.QueryFirstOrDefaultAsync<Settings>(query, new { Id = id });
    }

    /// <summary>
    /// Tüm ayarları getir
    /// </summary>
    public async Task<IEnumerable<Settings>> GetAllAsync()
    {
        var query = "SELECT * FROM Settings";
        
        using var connection = context.CreateConnection();
        return await connection.QueryAsync<Settings>(query);
    }

    /// <summary>
    /// İlk (ana) ayarları getir (Cache destekli)
    /// </summary>
    public async Task<Settings?> GetFirstAsync()
    {
        // Önce cache'den kontrol et
        var cachedSettings = await cacheService.GetSettingsAsync();
        if (cachedSettings != null)
        {
            return cachedSettings;
        }

        var query = "SELECT TOP 1 * FROM Settings ORDER BY Id";
        
        using var connection = context.CreateConnection();
        var settings = await connection.QueryFirstOrDefaultAsync<Settings>(query);
        
        // Cache'e kaydet
        if (settings != null)
        {
            await cacheService.SetSettingsAsync(settings);
        }
        
        return settings;
    }

    /// <summary>
    /// Ayarları güncelle (Cache otomatik güncellenir)
    /// </summary>
    public async Task<bool> UpdateAsync(Settings settings)
    {
        var query = @"
            UPDATE Settings SET 
                UserName = @UserName,
                Password = @Password,
                CompanyName = @CompanyName,
                About = @About,
                Logo = @Logo,
                Email = @Email,
                PhoneNumber = @PhoneNumber,
                PhoneNumber2 = @PhoneNumber2,
                Fax = @Fax,
                WhatsApp = @WhatsApp,
                Address = @Address,
                City = @City,
                District = @District,
                PostalCode = @PostalCode,
                Country = @Country,
                Facebook = @Facebook,
                Instagram = @Instagram,
                Twitter = @Twitter,
                LinkedIn = @LinkedIn,
                YouTube = @YouTube,
                WorkingHours = @WorkingHours,
                MetaTitle = @MetaTitle,
                MetaDescription = @MetaDescription,
                MetaKeywords = @MetaKeywords,
                GoogleTag = @GoogleTag,
                GoogleTagManager = @GoogleTagManager,
                GoogleSiteVerification = @GoogleSiteVerification,
                GoogleShoppingMerchantId = @GoogleShoppingMerchantId,
                GoogleShoppingServiceAccountEmail = @GoogleShoppingServiceAccountEmail,
                GoogleShoppingServiceAccountKeyJson = @GoogleShoppingServiceAccountKeyJson,
                GoogleAnalyticsPropertyId = @GoogleAnalyticsPropertyId,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id";
        
        using var connection = context.CreateConnection();
        var result = await connection.ExecuteAsync(query, settings);
        
        // Cache'i temizle (InvalidateAllAsync zaten Admin/Settings/Index.cshtml.cs'de çağrılıyor)
        // Bu yüzden burada ayrıca temizlemeye gerek yok
        
        return result > 0;
    }

    /// <summary>
    /// Yeni ayar ekle (Cache otomatik güncellenir)
    /// </summary>
    public async Task<int> CreateAsync(Settings settings)
    {
        var query = @"
            INSERT INTO Settings 
            (UserName, Password, CompanyName, About, Logo, Email, PhoneNumber, PhoneNumber2, 
             Fax, WhatsApp, Address, City, District, PostalCode, Country, 
             Facebook, Instagram, Twitter, LinkedIn, YouTube, WorkingHours, 
             MetaTitle, MetaDescription, MetaKeywords, GoogleTag, GoogleTagManager, GoogleSiteVerification, 
             GoogleShoppingMerchantId, GoogleShoppingServiceAccountEmail, GoogleShoppingServiceAccountKeyJson, GoogleAnalyticsPropertyId, CreatedAt)
            VALUES 
            (@UserName, @Password, @CompanyName, @About, @Logo, @Email, @PhoneNumber, @PhoneNumber2, 
             @Fax, @WhatsApp, @Address, @City, @District, @PostalCode, @Country, 
             @Facebook, @Instagram, @Twitter, @LinkedIn, @YouTube, @WorkingHours, 
             @MetaTitle, @MetaDescription, @MetaKeywords, @GoogleTag, @GoogleTagManager, @GoogleSiteVerification, 
             @GoogleShoppingMerchantId, @GoogleShoppingServiceAccountEmail, @GoogleShoppingServiceAccountKeyJson, @GoogleAnalyticsPropertyId, @CreatedAt);
            SELECT CAST(SCOPE_IDENTITY() as int)";
        
        using var connection = context.CreateConnection();
        var id = await connection.ExecuteScalarAsync<int>(query, settings);
        
        // Cache'e yeni settings'i ekle
        settings.Id = id;
        await cacheService.SetSettingsAsync(settings);
        
        return id;
    }
}

