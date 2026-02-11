-- 003: Seed Categories and SubCategories (mega menu data)
-- Idempotent: inserts only when Slug does not exist.
-- Categories first, then SubCategories with CategoryId from parent slug.
-- Sadece ürün tipi kategorileri: Çok Satanlar vb. filtre/collection başlıkları eklenmez.

SET NOCOUNT ON;
GO

-- ========== CATEGORIES ==========
-- 1. Şişme Parklar
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'siseme-parklar')
    INSERT INTO [dbo].[Categories] ([Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
    VALUES (N'Şişme Parklar', N'siseme-parklar', NULL, 1, GETDATE(), 1);
GO

-- 2. Top Havuzları
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'top-havuzlari')
    INSERT INTO [dbo].[Categories] ([Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
    VALUES (N'Top Havuzları', N'top-havuzlari', NULL, 1, GETDATE(), 2);
GO

-- 3. Reklam Balonları
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'reklam-balonlari')
    INSERT INTO [dbo].[Categories] ([Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
    VALUES (N'Reklam Balonları', N'reklam-balonlari', NULL, 1, GETDATE(), 3);
GO

-- 4. Şişme Çadırlar
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'siseme-cadirlar')
    INSERT INTO [dbo].[Categories] ([Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
    VALUES (N'Şişme Çadırlar', N'siseme-cadirlar', NULL, 1, GETDATE(), 4);
GO

-- 5. Trambolinler
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'trambolinler')
    INSERT INTO [dbo].[Categories] ([Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
    VALUES (N'Trambolinler', N'trambolinler', NULL, 1, GETDATE(), 5);
GO

-- 6. Çocuk Parkları
IF NOT EXISTS (SELECT 1 FROM [dbo].[Categories] WHERE [Slug] = N'cocuk-parklar')
    INSERT INTO [dbo].[Categories] ([Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
    VALUES (N'Çocuk Parkları', N'cocuk-parklar', NULL, 1, GETDATE(), 6);
GO

-- ========== SUB CATEGORIES ==========

-- ----- Şişme Parklar (siseme-parklar) -----
INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Havuz Oyunları', N'sisme-havuz-oyunlari', NULL, 1, GETDATE(), 1
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-havuz-oyunlari');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Rodeo', N'sisme-rodeo', NULL, 1, GETDATE(), 2
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-rodeo');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Oyun Parkı', N'sisme-oyun-parki', NULL, 1, GETDATE(), 3
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-oyun-parki');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'FunLand Kids Club', N'funland-kids-club', NULL, 1, GETDATE(), 4
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'funland-kids-club');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Tema Park', N'sisme-tema-park', NULL, 1, GETDATE(), 5
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-tema-park');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Survivor Parkur', N'dev-sisme-survivor-parkur', NULL, 1, GETDATE(), 6
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'dev-sisme-survivor-parkur');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Kaydırak', N'sisme-kaydirak', NULL, 1, GETDATE(), 7
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-kaydirak');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'İnteraktif Oyuncaklar', N'sisme-i-nteraktif-oyuncaklar', NULL, 1, GETDATE(), 8
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-i-nteraktif-oyuncaklar');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Havuzlar', N'sisme-havuzlar', NULL, 1, GETDATE(), 9
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-havuzlar');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Havuz', N'sisme-havuz', NULL, 1, GETDATE(), 10
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-havuz');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Eko Seri', N'sisme-oyun-eko-seri', NULL, 1, GETDATE(), 11
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-oyun-eko-seri');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Su Oyun Parkı', N'sisme-su-oyun-parki', NULL, 1, GETDATE(), 12
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-su-oyun-parki');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Pedallı Botlar', N'pedalli-botlar', NULL, 1, GETDATE(), 13
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'pedalli-botlar');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Waterball Su Topu', N'waterball-su-topu', NULL, 1, GETDATE(), 14
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'waterball-su-topu');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Oyun Parkı Aksesuarları', N'sisme-oyun-parki-aksesuarlari', NULL, 1, GETDATE(), 15
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-oyun-parki-aksesuarlari');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Küçük Boy Oyun Parkı', N'kucuk-boy-oyun-parki', NULL, 1, GETDATE(), 16
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'kucuk-boy-oyun-parki');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Orta Boy Oyun Parkı', N'orta-boy-oyun-parki', NULL, 1, GETDATE(), 17
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'orta-boy-oyun-parki');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Dev Oyun Parkı', N'dev-sisme-oyun-parki', NULL, 1, GETDATE(), 18
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'dev-sisme-oyun-parki');
GO

-- ----- Top Havuzları (top-havuzlari) -----
INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Tatami Minderleri', N'tatami-minderleri', NULL, 1, GETDATE(), 1
FROM [dbo].[Categories] c WHERE c.[Slug] = N'top-havuzlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'tatami-minderleri');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Top Havuzu', N'top-havuzu', NULL, 1, GETDATE(), 2
FROM [dbo].[Categories] c WHERE c.[Slug] = N'top-havuzlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'top-havuzu');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Orta Boy Top Havuzu', N'orta-boy-top-havuzu', NULL, 1, GETDATE(), 3
FROM [dbo].[Categories] c WHERE c.[Slug] = N'top-havuzlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'orta-boy-top-havuzu');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Dev Top Havuzları', N'dev-top-havuzlari', NULL, 1, GETDATE(), 4
FROM [dbo].[Categories] c WHERE c.[Slug] = N'top-havuzlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'dev-top-havuzlari');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'İç Mekan Oyun Parkları', N'i-c-mekan-oyun-parklari', NULL, 1, GETDATE(), 5
FROM [dbo].[Categories] c WHERE c.[Slug] = N'top-havuzlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'i-c-mekan-oyun-parklari');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Soft Play Survivor Parkur', N'soft-play-survivor-parkur', NULL, 1, GETDATE(), 6
FROM [dbo].[Categories] c WHERE c.[Slug] = N'top-havuzlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'soft-play-survivor-parkur');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Soft Play Oyuncaklar', N'soft-play-oyuncaklar', NULL, 1, GETDATE(), 7
FROM [dbo].[Categories] c WHERE c.[Slug] = N'top-havuzlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'soft-play-oyuncaklar');
GO

-- ----- Reklam Balonları (reklam-balonlari) -----
INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Sinevizyon', N'sisme-sinevizyon', NULL, 1, GETDATE(), 1
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-sinevizyon');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Balon Şişirme Pompası', N'otomotik-balon-sisirme-ponpasi', NULL, 1, GETDATE(), 2
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'otomotik-balon-sisirme-ponpasi');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Gel Gel Balon Adam', N'gel-gel-balon-adam', NULL, 1, GETDATE(), 3
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'gel-gel-balon-adam');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Kar Küresi', N'sisme-kar-kuresi', NULL, 1, GETDATE(), 4
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-kar-kuresi');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Yer Balonları', N'yer-balonlari', NULL, 1, GETDATE(), 5
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'yer-balonlari');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Uçan Balon Zeplin', N'ucan-balon-zeplin', NULL, 1, GETDATE(), 6
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'ucan-balon-zeplin');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Yol Takı', N'sisme-yol-taki', NULL, 1, GETDATE(), 7
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-yol-taki');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Fly Tüp', N'fly-tup', NULL, 1, GETDATE(), 8
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'fly-tup');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Kapadokya Balonu', N'kapadokya-balanu', NULL, 1, GETDATE(), 9
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'kapadokya-balanu');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Kostüm', N'sisme-kostum', NULL, 1, GETDATE(), 10
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-kostum');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Yılbaşı Ürünleri', N'sisme-yilbasi-urunleri', NULL, 1, GETDATE(), 11
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-yilbasi-urunleri');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Uçan Balon', N'ucan-balon', NULL, 1, GETDATE(), 12
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'ucan-balon');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Balon Tabela', N'sisme-balon-tabela', NULL, 1, GETDATE(), 13
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-balon-tabela');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Işıklı Balonlar', N'isikli-reklam-balonlari', NULL, 1, GETDATE(), 14
FROM [dbo].[Categories] c WHERE c.[Slug] = N'reklam-balonlari'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'isikli-reklam-balonlari');
GO

-- ----- Şişme Çadırlar (siseme-cadirlar) -----
INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Reklam Çadırları', N'sisme-reklam-cadirlari', NULL, 1, GETDATE(), 1
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-cadirlar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-reklam-cadirlari');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Planetaryum Çadırı', N'planetaryum-cadiri', NULL, 1, GETDATE(), 2
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-cadirlar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'planetaryum-cadiri');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'İlk Yardım Çadırı', N'i-lk-yardim-cadiri', NULL, 1, GETDATE(), 3
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-cadirlar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'i-lk-yardim-cadiri');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'İlk Müdahale Çadırı', N'i-lk-mudahale-cadiri', NULL, 1, GETDATE(), 4
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-cadirlar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'i-lk-mudahale-cadiri');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Deprem Çadırı', N'deprem-cadiri', NULL, 1, GETDATE(), 5
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-cadirlar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'deprem-cadiri');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Şişme Taziye Çadırı', N'sisme-taziye-cadiri', NULL, 1, GETDATE(), 6
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-cadirlar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'sisme-taziye-cadiri');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Bubble Tent', N'balon-cadir-bubble-tent', NULL, 1, GETDATE(), 7
FROM [dbo].[Categories] c WHERE c.[Slug] = N'siseme-cadirlar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'balon-cadir-bubble-tent');
GO

-- ----- Trambolinler (trambolinler) -----
INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'6''lı Olimpik Trambolin Park', N'6-li-olimpik-trambolin-park', NULL, 1, GETDATE(), 1
FROM [dbo].[Categories] c WHERE c.[Slug] = N'trambolinler'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'6-li-olimpik-trambolin-park');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'8''li Olimpik Trambolin', N'8-li-olimpik-trambolin', NULL, 1, GETDATE(), 2
FROM [dbo].[Categories] c WHERE c.[Slug] = N'trambolinler'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'8-li-olimpik-trambolin');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'10''lu Olimpik Trambolin', N'10-lu-olimpik-trambolin', NULL, 1, GETDATE(), 3
FROM [dbo].[Categories] c WHERE c.[Slug] = N'trambolinler'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'10-lu-olimpik-trambolin');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Bireysel Olimpik Trambolin', N'bireysel-olimpik-trambolin', NULL, 1, GETDATE(), 4
FROM [dbo].[Categories] c WHERE c.[Slug] = N'trambolinler'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'bireysel-olimpik-trambolin');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Step Trambolini', N'step-trambolini', NULL, 1, GETDATE(), 5
FROM [dbo].[Categories] c WHERE c.[Slug] = N'trambolinler'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'step-trambolini');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Junior Trambolin 4''lü', N'junior-trambolin-4-lu', NULL, 1, GETDATE(), 6
FROM [dbo].[Categories] c WHERE c.[Slug] = N'trambolinler'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'junior-trambolin-4-lu');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Olimpik Trambolin', N'ollimpik-trambolin', NULL, 1, GETDATE(), 7
FROM [dbo].[Categories] c WHERE c.[Slug] = N'trambolinler'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'ollimpik-trambolin');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'6''lı Junior Trambolin', N'6-li-junior-trambolin', NULL, 1, GETDATE(), 8
FROM [dbo].[Categories] c WHERE c.[Slug] = N'trambolinler'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'6-li-junior-trambolin');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'4''lü Olimpik Trambolin', N'4lu-olimpik-trambolin', NULL, 1, GETDATE(), 9
FROM [dbo].[Categories] c WHERE c.[Slug] = N'trambolinler'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'4lu-olimpik-trambolin');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Düz Trambolinler', N'duz-tranbolinler', NULL, 1, GETDATE(), 10
FROM [dbo].[Categories] c WHERE c.[Slug] = N'trambolinler'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'duz-tranbolinler');
GO

-- ----- Çocuk Parkları (cocuk-parklar) -----
INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Çocuk Oyun Parkları', N'cocuk-oyun-parklari', NULL, 1, GETDATE(), 1
FROM [dbo].[Categories] c WHERE c.[Slug] = N'cocuk-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'cocuk-oyun-parklari');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Oyun Parkı Ekipmanları', N'oyun-parki-ekipmanlari', NULL, 1, GETDATE(), 2
FROM [dbo].[Categories] c WHERE c.[Slug] = N'cocuk-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'oyun-parki-ekipmanlari');
GO

INSERT INTO [dbo].[SubCategories] ([CategoryId], [Name], [Slug], [Description], [IsActive], [CreatedAt], [DisplayOrder])
SELECT c.[Id], N'Kaydırak ve Salıncak', N'kaydirak-ve-salincak', NULL, 1, GETDATE(), 3
FROM [dbo].[Categories] c WHERE c.[Slug] = N'cocuk-parklar'
AND NOT EXISTS (SELECT 1 FROM [dbo].[SubCategories] WHERE [Slug] = N'kaydirak-ve-salincak');
GO
