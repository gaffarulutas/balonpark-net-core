-- 004: Seed 10 test products
-- Idempotent: inserts only when Product slug does not exist.
-- CategoryId/SubCategoryId resolved via SubCategories.Slug (requires 003 run first).

SET NOCOUNT ON;
GO

-- 1. Şişme Oyun Parkı - Mini Oyun Kalesi
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Slug] = N'mini-oyun-kalesi')
INSERT INTO [dbo].[Products] ([CategoryId], [SubCategoryId], [Name], [Slug], [Description], [TechnicalDescription], [Dimensions], [Price], [Stock], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], s.[Id], N'Mini Oyun Kalesi', N'mini-oyun-kalesi',
    N'Çocuklar için kompakt şişme oyun kalesi. Ev bahçesi ve küçük alanlar için uygundur.',
    NULL, N'3x3 m', 2499.00, 5, 1, GETDATE(), 1
FROM [dbo].[Categories] c
INNER JOIN [dbo].[SubCategories] s ON s.[CategoryId] = c.[Id]
WHERE c.[Slug] = N'siseme-parklar' AND s.[Slug] = N'sisme-oyun-parki';
GO

-- 2. Orta Boy Oyun Parkı - Aile Parkı
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Slug] = N'orta-boy-aile-parki')
INSERT INTO [dbo].[Products] ([CategoryId], [SubCategoryId], [Name], [Slug], [Description], [TechnicalDescription], [Dimensions], [Price], [Stock], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], s.[Id], N'Orta Boy Aile Parkı', N'orta-boy-aile-parki',
    N'Orta boy şişme oyun parkı. Doğum günü ve etkinlikler için idealdir.',
    NULL, N'5x5 m', 4599.00, 3, 1, GETDATE(), 2
FROM [dbo].[Categories] c
INNER JOIN [dbo].[SubCategories] s ON s.[CategoryId] = c.[Id]
WHERE c.[Slug] = N'siseme-parklar' AND s.[Slug] = N'orta-boy-oyun-parki';
GO

-- 3. Şişme Kaydırak - Çift Kulvar
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Slug] = N'siseme-cift-kulvar-kaydirak')
INSERT INTO [dbo].[Products] ([CategoryId], [SubCategoryId], [Name], [Slug], [Description], [TechnicalDescription], [Dimensions], [Price], [Stock], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], s.[Id], N'Çift Kulvar Şişme Kaydırak', N'siseme-cift-kulvar-kaydirak',
    N'İki kulvarlı şişme kaydırak. Parti ve etkinliklerde yarış keyfi.',
    NULL, N'4x6 m', 3299.00, 4, 1, GETDATE(), 3
FROM [dbo].[Categories] c
INNER JOIN [dbo].[SubCategories] s ON s.[CategoryId] = c.[Id]
WHERE c.[Slug] = N'siseme-parklar' AND s.[Slug] = N'sisme-kaydirak';
GO

-- 4. Şişme Havuz - Yuvarlak
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Slug] = N'siseme-yuvarlak-havuz')
INSERT INTO [dbo].[Products] ([CategoryId], [SubCategoryId], [Name], [Slug], [Description], [TechnicalDescription], [Dimensions], [Price], [Stock], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], s.[Id], N'Yuvarlak Şişme Havuz 3x1', N'siseme-yuvarlak-havuz',
    N'Yuvarlak tasarım şişme havuz. Yaz eğlencesi için güvenli ve dayanıklı.',
    NULL, N'Ø3x1 m', 899.00, 12, 1, GETDATE(), 4
FROM [dbo].[Categories] c
INNER JOIN [dbo].[SubCategories] s ON s.[CategoryId] = c.[Id]
WHERE c.[Slug] = N'siseme-parklar' AND s.[Slug] = N'sisme-havuz';
GO

-- 5. Top Havuzu - Standart
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Slug] = N'top-havuzu-standart')
INSERT INTO [dbo].[Products] ([CategoryId], [SubCategoryId], [Name], [Slug], [Description], [TechnicalDescription], [Dimensions], [Price], [Stock], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], s.[Id], N'Standart Top Havuzu', N'top-havuzu-standart',
    N'İç mekan için standart top havuzu. Renkli toplar dahildir.',
    NULL, N'2x2 m', 3499.00, 6, 1, GETDATE(), 5
FROM [dbo].[Categories] c
INNER JOIN [dbo].[SubCategories] s ON s.[CategoryId] = c.[Id]
WHERE c.[Slug] = N'top-havuzlari' AND s.[Slug] = N'top-havuzu';
GO

-- 6. Soft Play Survivor Parkur
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Slug] = N'soft-play-survivor-parkur')
INSERT INTO [dbo].[Products] ([CategoryId], [SubCategoryId], [Name], [Slug], [Description], [TechnicalDescription], [Dimensions], [Price], [Stock], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], s.[Id], N'Soft Play Survivor Parkur', N'soft-play-survivor-parkur',
    N'İç mekan survivor parkur. Engeller ve tırmanma elemanları ile eğlenceli oyun.',
    NULL, N'6x4 m', 12500.00, 2, 1, GETDATE(), 6
FROM [dbo].[Categories] c
INNER JOIN [dbo].[SubCategories] s ON s.[CategoryId] = c.[Id]
WHERE c.[Slug] = N'top-havuzlari' AND s.[Slug] = N'soft-play-survivor-parkur';
GO

-- 7. Gel Gel Balon Adam
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Slug] = N'gel-gel-balon-adam')
INSERT INTO [dbo].[Products] ([CategoryId], [SubCategoryId], [Name], [Slug], [Description], [TechnicalDescription], [Dimensions], [Price], [Stock], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], s.[Id], N'Gel Gel Balon Adam', N'gel-gel-balon-adam',
    N'Reklam ve etkinlikler için şişme balon adam. Rüzgarda hareket eden dikkat çekici ürün.',
    NULL, N'2.5 m', 2499.00, 8, 1, GETDATE(), 7
FROM [dbo].[Categories] c
INNER JOIN [dbo].[SubCategories] s ON s.[CategoryId] = c.[Id]
WHERE c.[Slug] = N'reklam-balonlari' AND s.[Slug] = N'gel-gel-balon-adam';
GO

-- 8. Şişme Reklam Çadırı
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Slug] = N'siseme-reklam-cadiri-3x3')
INSERT INTO [dbo].[Products] ([CategoryId], [SubCategoryId], [Name], [Slug], [Description], [TechnicalDescription], [Dimensions], [Price], [Stock], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], s.[Id], N'Şişme Reklam Çadırı 3x3', N'siseme-reklam-cadiri-3x3',
    N'Fuarlar ve açık hava etkinlikleri için şişme reklam çadırı. Hızlı kurulum.',
    NULL, N'3x3 m', 8999.00, 3, 1, GETDATE(), 8
FROM [dbo].[Categories] c
INNER JOIN [dbo].[SubCategories] s ON s.[CategoryId] = c.[Id]
WHERE c.[Slug] = N'siseme-cadirlar' AND s.[Slug] = N'sisme-reklam-cadirlari';
GO

-- 9. 4''lü Olimpik Trambolin
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Slug] = N'olimpik-4lu-trambolin')
INSERT INTO [dbo].[Products] ([CategoryId], [SubCategoryId], [Name], [Slug], [Description], [TechnicalDescription], [Dimensions], [Price], [Stock], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], s.[Id], N'4''lü Olimpik Trambolin', N'olimpik-4lu-trambolin',
    N'Profesyonel 4''lü olimpik trambolin seti. Kulüp ve spor salonu kullanımına uygundur.',
    NULL, N'4 üniteli', 45000.00, 1, 1, GETDATE(), 9
FROM [dbo].[Categories] c
INNER JOIN [dbo].[SubCategories] s ON s.[CategoryId] = c.[Id]
WHERE c.[Slug] = N'trambolinler' AND s.[Slug] = N'4lu-olimpik-trambolin';
GO

-- 10. Kaydırak ve Salıncak Seti
IF NOT EXISTS (SELECT 1 FROM [dbo].[Products] WHERE [Slug] = N'cocuk-kaydirak-salincak-seti')
INSERT INTO [dbo].[Products] ([CategoryId], [SubCategoryId], [Name], [Slug], [Description], [TechnicalDescription], [Dimensions], [Price], [Stock], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], s.[Id], N'Kaydırak ve Salıncak Seti', N'cocuk-kaydirak-salincak-seti',
    N'Çocuk parkı için kaydırak ve salıncak kombinasyonu. Dayanıklı metal konstrüksiyon.',
    NULL, N'4x3 m', 15999.00, 2, 1, GETDATE(), 10
FROM [dbo].[Categories] c
INNER JOIN [dbo].[SubCategories] s ON s.[CategoryId] = c.[Id]
WHERE c.[Slug] = N'cocuk-parklar' AND s.[Slug] = N'kaydirak-ve-salincak';
GO
