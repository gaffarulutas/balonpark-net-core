-- 019: Ürün YouTube videosu. Kapak galeride gösterilir; YouTubeShowFirst videoyu ilk sıraya alır.

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
BEGIN
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'YouTubeUrl', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [YouTubeUrl] [nvarchar](200) NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'YouTubeShowFirst', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [YouTubeShowFirst] [bit] NOT NULL CONSTRAINT [DF_Products_YouTubeShowFirst] DEFAULT (0);
END
GO
