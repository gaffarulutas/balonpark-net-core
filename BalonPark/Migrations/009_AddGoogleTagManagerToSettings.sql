-- Google Tag Manager (Container ID) ayarı
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'GoogleTagManager')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [GoogleTagManager] [nvarchar](50) NULL;
END
GO
