-- Vitrinde fiyat gösterimi ve uzaktaki fiyat değişikliklerini önbelleğe yansıtmak için revizyon
IF COL_LENGTH('dbo.Settings', 'ShowPrices') IS NULL
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [ShowPrices] [bit] NOT NULL CONSTRAINT [DF_Settings_ShowPrices] DEFAULT (1);
END
GO
IF COL_LENGTH('dbo.Settings', 'PriceRevision') IS NULL
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [PriceRevision] [datetime2](7) NOT NULL CONSTRAINT [DF_Settings_PriceRevision] DEFAULT (SYSDATETIME());
END
GO
