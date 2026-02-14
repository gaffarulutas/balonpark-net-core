-- 015: Ürün teknik bilgileri küsüratlı değer (decimal) desteği
-- AssemblyTime: int -> decimal(10,2) (örn: 1,5 saat)
-- MaterialWeightGrm2: int -> decimal(10,2) (örn: 650,5 gr/m²)
-- FanWeightKg, PackagedWeightKg, InflatedWeightKg zaten decimal(10,2)

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- AssemblyTime: geçici decimal sütunu ekle
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'AssemblyTime', 'ColumnId') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'AssemblyTimeDecimal', 'ColumnId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [AssemblyTimeDecimal] [decimal](10, 2) NULL;
END
GO

-- AssemblyTime: veriyi kopyala (int -> decimal)
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'AssemblyTimeDecimal', 'ColumnId') IS NOT NULL
BEGIN
    UPDATE [dbo].[Products] SET [AssemblyTimeDecimal] = TRY_CAST([AssemblyTime] AS decimal(10,2)) WHERE [AssemblyTime] IS NOT NULL;
END
GO

-- AssemblyTime: eski sütunu kaldır ve yeniden adlandır
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'AssemblyTimeDecimal', 'ColumnId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Products] DROP COLUMN [AssemblyTime];
    EXEC sp_rename 'dbo.Products.AssemblyTimeDecimal', 'AssemblyTime', 'COLUMN';
END
GO

-- MaterialWeightGrm2: geçici decimal sütunu ekle
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'MaterialWeightGrm2', 'ColumnId') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'MaterialWeightGrm2Decimal', 'ColumnId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [MaterialWeightGrm2Decimal] [decimal](10, 2) NULL;
END
GO

-- MaterialWeightGrm2: veriyi kopyala (int -> decimal)
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'MaterialWeightGrm2Decimal', 'ColumnId') IS NOT NULL
BEGIN
    UPDATE [dbo].[Products] SET [MaterialWeightGrm2Decimal] = TRY_CAST([MaterialWeightGrm2] AS decimal(10,2)) WHERE [MaterialWeightGrm2] IS NOT NULL;
END
GO

-- MaterialWeightGrm2: eski sütunu kaldır ve yeniden adlandır
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'MaterialWeightGrm2Decimal', 'ColumnId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Products] DROP COLUMN [MaterialWeightGrm2];
    EXEC sp_rename 'dbo.Products.MaterialWeightGrm2Decimal', 'MaterialWeightGrm2', 'COLUMN';
END
GO
