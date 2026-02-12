-- 011: Ürün detay sayfası etiketleri ve özellik alanları
-- Badge'ler (İndirimli, Popüler, Projeye özel), Teslimat, Ateşe dayanıklı, Kumaş, Renk, Şişmiş ağırlık

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
BEGIN
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'IsDiscounted', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [IsDiscounted] [bit] NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'IsPopular', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [IsPopular] [bit] NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'IsProjectSpecial', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [IsProjectSpecial] [bit] NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'DeliveryDays', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [DeliveryDays] [nvarchar](100) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'IsFireResistant', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [IsFireResistant] [bit] NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'MaterialWeight', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [MaterialWeight] [nvarchar](100) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'ColorOptions', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [ColorOptions] [nvarchar](200) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'InflatedWeightKg', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [InflatedWeightKg] [decimal](10, 2) NULL;
END
GO
