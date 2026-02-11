-- 006: Ürün Teknik Bilgiler - Montaj süresi (saat) ve gerekli kişi sayısı integer
-- AssemblyTime: nvarchar -> int (saat), RequiredPersonCount: nvarchar -> int
-- Her ALTER/UPDATE ayrı batch (GO) ile çalıştırılır; yoksa yeni sütun aynı batch'te tanınmaz.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- AssemblyTime: geçici sütun ekle (batch 1)
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'AssemblyTime', 'ColumnId') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'AssemblyTimeHours', 'ColumnId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [AssemblyTimeHours] [int] NULL;
END
GO

-- AssemblyTime: veriyi kopyala (batch 2 - yeni sütun artık var)
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'AssemblyTimeHours', 'ColumnId') IS NOT NULL
BEGIN
    UPDATE [dbo].[Products] SET [AssemblyTimeHours] = TRY_CAST([AssemblyTime] AS int) WHERE [AssemblyTime] IS NOT NULL;
END
GO

-- AssemblyTime: eski sütunu kaldır ve yeniden adlandır (batch 3)
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'AssemblyTimeHours', 'ColumnId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Products] DROP COLUMN [AssemblyTime];
    EXEC sp_rename 'dbo.Products.AssemblyTimeHours', 'AssemblyTime', 'COLUMN';
END
GO

-- RequiredPersonCount: geçici sütun ekle (batch 4)
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'RequiredPersonCount', 'ColumnId') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'RequiredPersonCountInt', 'ColumnId') IS NULL
BEGIN
    ALTER TABLE [dbo].[Products] ADD [RequiredPersonCountInt] [int] NULL;
END
GO

-- RequiredPersonCount: veriyi kopyala (batch 5)
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'RequiredPersonCountInt', 'ColumnId') IS NOT NULL
BEGIN
    UPDATE [dbo].[Products] SET [RequiredPersonCountInt] = TRY_CAST([RequiredPersonCount] AS int) WHERE [RequiredPersonCount] IS NOT NULL;
END
GO

-- RequiredPersonCount: eski sütunu kaldır ve yeniden adlandır (batch 6)
IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
   AND COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'RequiredPersonCountInt', 'ColumnId') IS NOT NULL
BEGIN
    ALTER TABLE [dbo].[Products] DROP COLUMN [RequiredPersonCount];
    EXEC sp_rename 'dbo.Products.RequiredPersonCountInt', 'RequiredPersonCount', 'COLUMN';
END
GO
