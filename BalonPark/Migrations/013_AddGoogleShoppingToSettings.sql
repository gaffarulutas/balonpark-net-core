-- Google Shopping API ayarları (Merchant ID, Service Account Email, JSON Key)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'GoogleShoppingMerchantId')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [GoogleShoppingMerchantId] [nvarchar](50) NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'GoogleShoppingServiceAccountEmail')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [GoogleShoppingServiceAccountEmail] [nvarchar](255) NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'GoogleShoppingServiceAccountKeyJson')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [GoogleShoppingServiceAccountKeyJson] [nvarchar](max) NULL;
END
GO
