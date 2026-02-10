-- 002: DisplayOrder kolonu yoksa ekle (eski şema uyumluluğu)
-- Idempotent: kolon varsa dokunmaz. ADD ve UPDATE/INDEX ayrı batch (SQL Server derleme kuralı).

IF COL_LENGTH('dbo.Categories', 'DisplayOrder') IS NULL
    ALTER TABLE [dbo].[Categories] ADD [DisplayOrder] INT NOT NULL DEFAULT 0
GO
IF COL_LENGTH('dbo.Categories', 'DisplayOrder') IS NOT NULL
BEGIN
    UPDATE [dbo].[Categories] SET [DisplayOrder] = [Id] WHERE [DisplayOrder] = 0
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Categories_DisplayOrder' AND object_id = OBJECT_ID('dbo.Categories'))
        CREATE NONCLUSTERED INDEX [IX_Categories_DisplayOrder] ON [dbo].[Categories]([DisplayOrder] ASC)
END
GO

IF COL_LENGTH('dbo.SubCategories', 'DisplayOrder') IS NULL
    ALTER TABLE [dbo].[SubCategories] ADD [DisplayOrder] INT NOT NULL DEFAULT 0
GO
IF COL_LENGTH('dbo.SubCategories', 'DisplayOrder') IS NOT NULL
BEGIN
    UPDATE [dbo].[SubCategories] SET [DisplayOrder] = [Id] WHERE [DisplayOrder] = 0
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SubCategories_DisplayOrder' AND object_id = OBJECT_ID('dbo.SubCategories'))
        CREATE NONCLUSTERED INDEX [IX_SubCategories_DisplayOrder] ON [dbo].[SubCategories]([DisplayOrder] ASC)
END
GO

IF COL_LENGTH('dbo.Products', 'DisplayOrder') IS NULL
    ALTER TABLE [dbo].[Products] ADD [DisplayOrder] INT NOT NULL DEFAULT 0
GO
IF COL_LENGTH('dbo.Products', 'DisplayOrder') IS NOT NULL
BEGIN
    UPDATE [dbo].[Products] SET [DisplayOrder] = [Id] WHERE [DisplayOrder] = 0
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Products_DisplayOrder' AND object_id = OBJECT_ID('dbo.Products'))
        CREATE NONCLUSTERED INDEX [IX_Products_DisplayOrder] ON [dbo].[Products]([DisplayOrder] ASC)
END
GO
