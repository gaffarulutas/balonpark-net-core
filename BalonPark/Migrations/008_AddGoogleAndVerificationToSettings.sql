-- Google Tag (gtag.js Ölçüm ID) ve Google Site Verification ayarları
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'GoogleTag')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [GoogleTag] [nvarchar](100) NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'GoogleSiteVerification')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [GoogleSiteVerification] [nvarchar](255) NULL;
END
GO
