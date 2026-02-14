-- Google Analytics 4 Property ID (dashboard raporları için, veritabanına kaydetme yok - anlık API)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Settings') AND name = 'GoogleAnalyticsPropertyId')
BEGIN
    ALTER TABLE [dbo].[Settings] ADD [GoogleAnalyticsPropertyId] [nvarchar](20) NULL;
END
GO
