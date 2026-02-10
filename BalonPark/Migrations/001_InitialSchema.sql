-- 001: Initial schema
-- Tablolar: Categories, Settings, SubCategories, Products, ProductImages, Blogs
-- Sıra: FK bağımlılıklarına göre (Categories -> Settings -> SubCategories -> Products -> ProductImages; Blogs bağımsız)

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Categories
IF OBJECT_ID('dbo.Categories', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Categories](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [UpdatedAt] [datetime2](7) NULL,
        [Slug] [nvarchar](200) NOT NULL,
        [DisplayOrder] [int] NOT NULL,
        PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    CREATE NONCLUSTERED INDEX [IX_Categories_IsActive] ON [dbo].[Categories]([IsActive] ASC)
    CREATE NONCLUSTERED INDEX [IX_Categories_Name] ON [dbo].[Categories]([Name] ASC)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Categories_Slug] ON [dbo].[Categories]([Slug] ASC)
    CREATE NONCLUSTERED INDEX [IX_Categories_DisplayOrder] ON [dbo].[Categories]([DisplayOrder] ASC)
    ALTER TABLE [dbo].[Categories] ADD DEFAULT ((1)) FOR [IsActive]
    ALTER TABLE [dbo].[Categories] ADD DEFAULT (getdate()) FOR [CreatedAt]
    ALTER TABLE [dbo].[Categories] ADD DEFAULT ((0)) FOR [DisplayOrder]
END
GO

-- Settings
IF OBJECT_ID('dbo.Settings', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Settings](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserName] [varchar](50) NOT NULL,
        [Password] [varchar](255) NULL,
        [CompanyName] [nvarchar](200) NOT NULL,
        [About] [nvarchar](max) NULL,
        [Logo] [nvarchar](500) NULL,
        [Email] [varchar](100) NOT NULL,
        [PhoneNumber] [varchar](20) NULL,
        [PhoneNumber2] [varchar](20) NULL,
        [Fax] [varchar](20) NULL,
        [WhatsApp] [varchar](20) NULL,
        [Address] [nvarchar](500) NULL,
        [City] [nvarchar](100) NULL,
        [District] [nvarchar](100) NULL,
        [PostalCode] [varchar](10) NULL,
        [Country] [nvarchar](100) NULL,
        [Facebook] [varchar](255) NULL,
        [Instagram] [varchar](255) NULL,
        [Twitter] [varchar](255) NULL,
        [LinkedIn] [varchar](255) NULL,
        [YouTube] [varchar](255) NULL,
        [WorkingHours] [nvarchar](500) NULL,
        [MetaTitle] [nvarchar](200) NULL,
        [MetaDescription] [nvarchar](500) NULL,
        [MetaKeywords] [nvarchar](500) NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [UpdatedAt] [datetime2](7) NULL,
        PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    ALTER TABLE [dbo].[Settings] ADD DEFAULT (getdate()) FOR [CreatedAt]
    INSERT INTO [dbo].[Settings] ([UserName],[Password],[CompanyName],[About],[Email],[PhoneNumber],[PhoneNumber2],[WhatsApp],[Address],[City],[District],[PostalCode],[Country],[Facebook],[Instagram],[Twitter],[LinkedIn],[WorkingHours],[MetaTitle],[MetaDescription],[MetaKeywords],[CreatedAt])
    VALUES ('admin','admin123','Balon Park Şişme Oyun Grubu','Balon Park, şişme oyun grupları konusunda uzmanlaşmış, kaliteli ve güvenli ürünler sunan öncü bir şişme oyun grubu üreticisidir.','info@balonpark.com','+90 212 555 01 23','+90 212 555 01 24','+90 532 555 01 23','Organize Sanayi Bölgesi 5. Cadde No:42','İstanbul','Başakşehir','34480','Türkiye','https://facebook.com/balonpark','https://instagram.com/balonpark','https://twitter.com/balonpark','https://linkedin.com/company/balonpark','Pazartesi-Cuma: 09:00-18:00, Cumartesi: 09:00-14:00, Pazar: Kapalı','Balon Park - Şişme Oyun Grubu Üreticisi','Kaliteli şişme oyun parkları, şişme kaydırak ve eğlence ürünleri.','şişme oyun parkı, şişme kaydırak, şişme havuz, şişme rodeo, top havuzu, çocuk oyun grupları',GETDATE())
END
GO

-- SubCategories (Categories'e bağlı)
IF OBJECT_ID('dbo.SubCategories', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[SubCategories](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CategoryId] [int] NOT NULL,
        [Name] [nvarchar](100) NOT NULL,
        [Description] [nvarchar](500) NULL,
        [IsActive] [bit] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [UpdatedAt] [datetime2](7) NULL,
        [Slug] [nvarchar](200) NOT NULL,
        [DisplayOrder] [int] NOT NULL,
        PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_SubCategories_Categories] FOREIGN KEY([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE CASCADE
    )
    CREATE NONCLUSTERED INDEX [IX_SubCategories_CategoryId] ON [dbo].[SubCategories]([CategoryId] ASC)
    CREATE NONCLUSTERED INDEX [IX_SubCategories_IsActive] ON [dbo].[SubCategories]([IsActive] ASC)
    CREATE NONCLUSTERED INDEX [IX_SubCategories_Name] ON [dbo].[SubCategories]([Name] ASC)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_SubCategories_Slug] ON [dbo].[SubCategories]([Slug] ASC)
    CREATE NONCLUSTERED INDEX [IX_SubCategories_DisplayOrder] ON [dbo].[SubCategories]([DisplayOrder] ASC)
    ALTER TABLE [dbo].[SubCategories] ADD DEFAULT ((1)) FOR [IsActive]
    ALTER TABLE [dbo].[SubCategories] ADD DEFAULT (getdate()) FOR [CreatedAt]
    ALTER TABLE [dbo].[SubCategories] ADD DEFAULT ((0)) FOR [DisplayOrder]
END
GO

-- Products (Categories ve SubCategories'e bağlı)
IF OBJECT_ID('dbo.Products', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[Products](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [CategoryId] [int] NOT NULL,
        [SubCategoryId] [int] NOT NULL,
        [Name] [nvarchar](200) NOT NULL,
        [Slug] [nvarchar](300) NOT NULL,
        [Description] [nvarchar](max) NOT NULL,
        [TechnicalDescription] [nvarchar](max) NULL,
        [Dimensions] [nvarchar](100) NULL,
        [Price] [decimal](18, 2) NOT NULL,
        [Stock] [int] NOT NULL,
        [IsActive] [bit] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        [UpdatedAt] [datetime2](7) NULL,
        [DisplayOrder] [int] NOT NULL,
        PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Products_Categories] FOREIGN KEY([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE NO ACTION ON UPDATE NO ACTION,
        CONSTRAINT [FK_Products_SubCategories] FOREIGN KEY([SubCategoryId]) REFERENCES [dbo].[SubCategories]([Id]) ON DELETE CASCADE
    )
    CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [dbo].[Products]([CategoryId] ASC)
    CREATE NONCLUSTERED INDEX [IX_Products_SubCategoryId] ON [dbo].[Products]([SubCategoryId] ASC)
    CREATE NONCLUSTERED INDEX [IX_Products_IsActive] ON [dbo].[Products]([IsActive] ASC)
    CREATE NONCLUSTERED INDEX [IX_Products_Name] ON [dbo].[Products]([Name] ASC)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Products_Slug] ON [dbo].[Products]([Slug] ASC)
    CREATE NONCLUSTERED INDEX [IX_Products_DisplayOrder] ON [dbo].[Products]([DisplayOrder] ASC)
    ALTER TABLE [dbo].[Products] ADD DEFAULT ((1)) FOR [IsActive]
    ALTER TABLE [dbo].[Products] ADD DEFAULT ((0)) FOR [Stock]
    ALTER TABLE [dbo].[Products] ADD DEFAULT (getdate()) FOR [CreatedAt]
    ALTER TABLE [dbo].[Products] ADD DEFAULT ((0)) FOR [DisplayOrder]
END
GO

-- ProductImages (Products'a bağlı - Products'tan sonra oluşturulmalı)
IF OBJECT_ID('dbo.ProductImages', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[ProductImages](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [ProductId] [int] NOT NULL,
        [FileName] [nvarchar](255) NOT NULL,
        [OriginalPath] [nvarchar](500) NOT NULL,
        [LargePath] [nvarchar](500) NOT NULL,
        [ThumbnailPath] [nvarchar](500) NOT NULL,
        [IsMainImage] [bit] NOT NULL,
        [DisplayOrder] [int] NOT NULL,
        [CreatedAt] [datetime2](7) NOT NULL,
        PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_ProductImages_Products] FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products]([Id]) ON DELETE CASCADE
    )
    CREATE NONCLUSTERED INDEX [IX_ProductImages_DisplayOrder] ON [dbo].[ProductImages]([DisplayOrder] ASC)
    CREATE NONCLUSTERED INDEX [IX_ProductImages_IsMainImage] ON [dbo].[ProductImages]([IsMainImage] ASC)
    CREATE NONCLUSTERED INDEX [IX_ProductImages_ProductId] ON [dbo].[ProductImages]([ProductId] ASC)
    ALTER TABLE [dbo].[ProductImages] ADD DEFAULT ((0)) FOR [IsMainImage]
    ALTER TABLE [dbo].[ProductImages] ADD DEFAULT ((0)) FOR [DisplayOrder]
    ALTER TABLE [dbo].[ProductImages] ADD DEFAULT (getdate()) FOR [CreatedAt]
END
GO

-- Blogs (bağımsız tablo - FK yok)
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
    ALTER TABLE [dbo].[Blogs] ADD DEFAULT ('Balon Park') FOR [AuthorName]
END
GO

-- Blog seed (sadece tablo boşsa)
IF OBJECT_ID('dbo.Blogs', 'U') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM [dbo].[Blogs])
BEGIN
    INSERT INTO [dbo].[Blogs] ([Title], [Slug], [Content], [Excerpt], [FeaturedImage], [MetaTitle], [MetaDescription], [MetaKeywords], [IsActive], [IsFeatured], [ViewCount], [CreatedAt], [PublishedAt], [AuthorName], [Category])
    VALUES
    ('Şişme Oyun Parkları Hakkında Bilmeniz Gerekenler', 'sisime-oyun-parklari-hakkinda-bilmeniz-gerekenler', '<h2>Şişme Oyun Parkları Nedir?</h2><p>Şişme oyun parkları, çocukların güvenli bir şekilde eğlenebileceği, dayanıklı malzemelerden üretilen eğlence alanlarıdır.</p>', 'Şişme oyun parkları hakkında detaylı bilgi edinin.', '/assets/images/blog/sisime-oyun-parklari.jpg', 'Şişme Oyun Parkları | Balon Park', 'Şişme oyun parkları hakkında detaylı bilgi.', 'şişme oyun parkı, çocuk oyun alanı', 1, 1, 0, GETDATE(), GETDATE(), 'Balon Park', 'Şişme Oyun Parkları'),
    ('Çocuklar İçin En İyi Şişme Kaydırak Seçenekleri', 'cocuklar-icin-en-iyi-sisime-kaydirak-secenekleri', '<h2>Şişme Kaydırak Çeşitleri</h2><p>Çocuklar için en popüler eğlence araçlarından biri olan şişme kaydıraklar.</p>', 'Çocuklar için en iyi şişme kaydırak seçenekleri.', '/assets/images/blog/sisime-kaydirak.jpg', 'Şişme Kaydırak | Balon Park', 'Şişme kaydırak seçenekleri.', 'şişme kaydırak, çocuk kaydırak', 1, 1, 0, GETDATE(), GETDATE(), 'Balon Park', 'Şişme Kaydıraklar'),
    ('Şişme Havuzların Bakımı ve Temizliği', 'sisime-havuzlarin-bakimi-ve-temizligi', '<h2>Şişme Havuz Bakım Rehberi</h2><p>Şişme havuzların uzun ömürlü olması için düzenli bakım şarttır.</p>', 'Şişme havuzların bakımı ve temizliği rehberi.', '/assets/images/blog/sisime-havuz-bakim.jpg', 'Şişme Havuz Bakımı | Balon Park', 'Şişme havuz bakım rehberi.', 'şişme havuz bakımı, havuz temizliği', 1, 0, 0, GETDATE(), GETDATE(), 'Balon Park', 'Bakım ve Temizlik')
END
GO
