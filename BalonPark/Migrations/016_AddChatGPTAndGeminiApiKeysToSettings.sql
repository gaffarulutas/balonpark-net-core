-- ChatGPT ve Gemini API anahtarları (Admin ayarlardan yönetilir, appsettings'den kaldırıldı)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'ChatGPTApiKey')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [ChatGPTApiKey] [nvarchar](500) NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'GeminiApiKey')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [GeminiApiKey] [nvarchar](500) NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'GeminiUseImagenFallback')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [GeminiUseImagenFallback] [bit] NULL;
END
GO
