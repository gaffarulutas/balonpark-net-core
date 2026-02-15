-- Yandex Webmaster site doğrulama (Admin ayarlardan güncellenir)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'YandexSiteVerification')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [YandexSiteVerification] [nvarchar](255) NULL;
END
GO
