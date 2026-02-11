-- Replace Dimensions with Summary (Özet) for product short summary.
-- Ölçüler removed; technical dimensions remain in Ürün Teknik Bilgileri (InflatedLength/Width/Height, PackagedLength/Depth, etc.).
-- Her adım ayrı batch (GO) ile çalıştırılır; yoksa yeni sütun aynı batch'te tanınmaz.

-- 1. Summary sütununu ekle
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'Summary')
BEGIN
    ALTER TABLE [dbo].[Products] ADD [Summary] [nvarchar](500) NULL;
END
GO

-- 2. Mevcut Dimensions verisini Summary'ye kopyala (örn. seed "3x3 m")
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'Dimensions')
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'Summary')
BEGIN
    UPDATE [dbo].[Products] SET [Summary] = [Dimensions] WHERE [Dimensions] IS NOT NULL AND ([Summary] IS NULL OR [Summary] = N'');
END
GO

-- 3. Dimensions sütununu kaldır
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Products') AND name = 'Dimensions')
BEGIN
    ALTER TABLE [dbo].[Products] DROP COLUMN [Dimensions];
END
GO
