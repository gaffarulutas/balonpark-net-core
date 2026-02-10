-- 003: Categories, SubCategories ve Products tablolarına DisplayOrder kolonu

-- Categories
IF COL_LENGTH('dbo.Categories', 'DisplayOrder') IS NULL
BEGIN
    ALTER TABLE [dbo].[Categories] ADD [DisplayOrder] INT NOT NULL DEFAULT 0
    UPDATE [dbo].[Categories] SET [DisplayOrder] = [Id]
    CREATE NONCLUSTERED INDEX [IX_Categories_DisplayOrder] ON [dbo].[Categories]([DisplayOrder] ASC)
END
GO

-- SubCategories
IF COL_LENGTH('dbo.SubCategories', 'DisplayOrder') IS NULL
BEGIN
    ALTER TABLE [dbo].[SubCategories] ADD [DisplayOrder] INT NOT NULL DEFAULT 0
    UPDATE [dbo].[SubCategories] SET [DisplayOrder] = [Id]
    CREATE NONCLUSTERED INDEX [IX_SubCategories_DisplayOrder] ON [dbo].[SubCategories]([DisplayOrder] ASC)
END
GO

-- Products
IF COL_LENGTH('dbo.Products', 'DisplayOrder') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [DisplayOrder] INT NOT NULL DEFAULT 0
    UPDATE [dbo].[Products] SET [DisplayOrder] = [Id]
    CREATE NONCLUSTERED INDEX [IX_Products_DisplayOrder] ON [dbo].[Products]([DisplayOrder] ASC)
END
GO
