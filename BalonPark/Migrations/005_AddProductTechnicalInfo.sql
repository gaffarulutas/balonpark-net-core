-- 005: Ürün Teknik Bilgiler (Ürün Teknik Bilgiler ekranı alanları)
-- Şişmiş ürün, Montaj/demontaj, Paketlenmiş ürün, Genel

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
BEGIN
    -- Şişmiş ürün (Inflated product)
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'InflatedLength', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [InflatedLength] [nvarchar](50) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'InflatedWidth', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [InflatedWidth] [nvarchar](50) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'InflatedHeight', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [InflatedHeight] [nvarchar](50) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'UserCount', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [UserCount] [int] NULL;

    -- Montaj / demontaj (Assembly / disassembly)
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'AssemblyTime', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [AssemblyTime] [nvarchar](100) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'RequiredPersonCount', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [RequiredPersonCount] [nvarchar](50) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'FanDescription', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [FanDescription] [nvarchar](200) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'FanWeightKg', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [FanWeightKg] [decimal](10, 2) NULL;

    -- Paketlenmiş ürünün özellikleri (Packaged product features)
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'PackagedLength', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [PackagedLength] [nvarchar](50) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'PackagedDepth', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [PackagedDepth] [nvarchar](50) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'PackagedWeightKg', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [PackagedWeightKg] [decimal](10, 2) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'PackagePalletCount', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [PackagePalletCount] [int] NULL;

    -- Genel (General)
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'HasCertificate', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [HasCertificate] [bit] NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'WarrantyDescription', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [WarrantyDescription] [nvarchar](100) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'AfterSalesService', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [AfterSalesService] [nvarchar](500) NULL;
END
GO
