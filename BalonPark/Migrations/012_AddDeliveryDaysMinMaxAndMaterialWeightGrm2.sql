-- 012: Teslimat (min-max iş günü) ve kumaş ağırlığı (gr/m²) integer alanları
-- Detay sayfasında metin otomatik oluşturulur: "X-Y iş günü", "X gr/m² kumaş"

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.Products', 'U') IS NOT NULL
BEGIN
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'DeliveryDaysMin', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [DeliveryDaysMin] [int] NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'DeliveryDaysMax', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [DeliveryDaysMax] [int] NULL;
    IF COLUMNPROPERTY(OBJECT_ID('dbo.Products'), 'MaterialWeightGrm2', 'ColumnId') IS NULL
        ALTER TABLE [dbo].[Products] ADD [MaterialWeightGrm2] [int] NULL;
END
GO
