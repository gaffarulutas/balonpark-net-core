-- 002: Blog tablosu
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF OBJECT_ID('dbo.Blogs', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Blogs](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Title] [nvarchar](200) NOT NULL,
        [Slug] [nvarchar](250) NOT NULL,
        [Content] [nvarchar](max) NOT NULL,
        [Excerpt] [nvarchar](500) NULL,
        [FeaturedImage] [nvarchar](500) NULL,
        [MetaTitle] [nvarchar](200) NULL,
        [MetaDescription] [nvarchar](300) NULL,
        [MetaKeywords] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL,
        [IsFeatured] [bit] NOT NULL,
        [ViewCount] [int] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [UpdatedAt] [datetime2](7) NULL,
        [PublishedAt] [datetime2](7) NULL,
        [AuthorName] [nvarchar](100) NULL,
        [Category] [nvarchar](100) NULL,
        PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    CREATE NONCLUSTERED INDEX [IX_Blogs_IsActive] ON [dbo].[Blogs]([IsActive] ASC)
    CREATE NONCLUSTERED INDEX [IX_Blogs_IsFeatured] ON [dbo].[Blogs]([IsFeatured] ASC)
    CREATE NONCLUSTERED INDEX [IX_Blogs_PublishedAt] ON [dbo].[Blogs]([PublishedAt] ASC)
    CREATE NONCLUSTERED INDEX [IX_Blogs_Category] ON [dbo].[Blogs]([Category] ASC)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Blogs_Slug] ON [dbo].[Blogs]([Slug] ASC)
    ALTER TABLE [dbo].[Blogs] ADD DEFAULT ((1)) FOR [IsActive]
    ALTER TABLE [dbo].[Blogs] ADD DEFAULT ((0)) FOR [IsFeatured]
    ALTER TABLE [dbo].[Blogs] ADD DEFAULT ((0)) FOR [ViewCount]
    ALTER TABLE [dbo].[Blogs] ADD DEFAULT (getdate()) FOR [CreatedAt]
    ALTER TABLE [dbo].[Blogs] ADD DEFAULT ('Ünlü Park') FOR [AuthorName]
END
GO

-- Örnek blog verileri (sadece tablo boşsa)
IF OBJECT_ID('dbo.Blogs', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Blogs])
BEGIN
    INSERT INTO [dbo].[Blogs] ([Title], [Slug], [Content], [Excerpt], [FeaturedImage], [MetaTitle], [MetaDescription], [MetaKeywords], [IsActive], [IsFeatured], [ViewCount], [CreatedAt], [PublishedAt], [AuthorName], [Category])
    VALUES
    ('Şişme Oyun Parkları Hakkında Bilmeniz Gerekenler', 'sisime-oyun-parklari-hakkinda-bilmeniz-gerekenler', '<h2>Şişme Oyun Parkları Nedir?</h2><p>Şişme oyun parkları, çocukların güvenli bir şekilde eğlenebileceği, dayanıklı malzemelerden üretilen eğlence alanlarıdır.</p>', 'Şişme oyun parkları hakkında detaylı bilgi edinin.', '/assets/images/blog/sisime-oyun-parklari.jpg', 'Şişme Oyun Parkları | Ünlü Park', 'Şişme oyun parkları hakkında detaylı bilgi.', 'şişme oyun parkı, çocuk oyun alanı', 1, 1, 0, GETDATE(), GETDATE(), 'Ünlü Park', 'Şişme Oyun Parkları'),
    ('Çocuklar İçin En İyi Şişme Kaydırak Seçenekleri', 'cocuklar-icin-en-iyi-sisime-kaydirak-secenekleri', '<h2>Şişme Kaydırak Çeşitleri</h2><p>Çocuklar için en popüler eğlence araçlarından biri olan şişme kaydıraklar.</p>', 'Çocuklar için en iyi şişme kaydırak seçenekleri.', '/assets/images/blog/sisime-kaydirak.jpg', 'Şişme Kaydırak | Ünlü Park', 'Şişme kaydırak seçenekleri.', 'şişme kaydırak, çocuk kaydırak', 1, 1, 0, GETDATE(), GETDATE(), 'Ünlü Park', 'Şişme Kaydıraklar'),
    ('Şişme Havuzların Bakımı ve Temizliği', 'sisime-havuzlarin-bakimi-ve-temizligi', '<h2>Şişme Havuz Bakım Rehberi</h2><p>Şişme havuzların uzun ömürlü olması için düzenli bakım şarttır.</p>', 'Şişme havuzların bakımı ve temizliği rehberi.', '/assets/images/blog/sisime-havuz-bakim.jpg', 'Şişme Havuz Bakımı | Ünlü Park', 'Şişme havuz bakım rehberi.', 'şişme havuz bakımı, havuz temizliği', 1, 0, 0, GETDATE(), GETDATE(), 'Ünlü Park', 'Bakım ve Temizlik')
END
GO
