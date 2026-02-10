-- 001: Initial schema - Categories, Settings, SubCategories, Products, ProductImages
-- Sıra: Categories -> Settings -> SubCategories -> Products -> ProductImages (FK bağımlılıkları)

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
        PRIMARY KEY CLUSTERED ([Id] ASC)
    )
    CREATE NONCLUSTERED INDEX [IX_Categories_IsActive] ON [dbo].[Categories]([IsActive] ASC)
    CREATE NONCLUSTERED INDEX [IX_Categories_Name] ON [dbo].[Categories]([Name] ASC)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Categories_Slug] ON [dbo].[Categories]([Slug] ASC)
    ALTER TABLE [dbo].[Categories] ADD DEFAULT ((1)) FOR [IsActive]
    ALTER TABLE [dbo].[Categories] ADD DEFAULT (getdate()) FOR [CreatedAt]
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
    VALUES ('admin','admin123','Ünlü Park Bahçe Mobilyaları','Ünlü Park, bahçe ve park mobilyaları konusunda uzmanlaşmış, kaliteli ve estetik ürünler sunan öncü bir şirkettir.','info@unlupark.com','+90 212 555 01 23','+90 212 555 01 24','+90 532 555 01 23','Organize Sanayi Bölgesi 5. Cadde No:42','İstanbul','Başakşehir','34480','Türkiye','https://facebook.com/unlupark','https://instagram.com/unlupark','https://twitter.com/unlupark','https://linkedin.com/company/unlupark','Pazartesi-Cuma: 09:00-18:00, Cumartesi: 09:00-14:00, Pazar: Kapalı','Ünlü Park - Bahçe ve Park Mobilyaları','Kaliteli bahçe mobilyaları, park ekipmanları ve dış mekan ürünleri.','bahçe mobilyaları, park mobilyaları, dış mekan, bahçe bankı, çöp kovası, saksı, pergola',GETDATE())
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
        PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_SubCategories_Categories] FOREIGN KEY([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE CASCADE
    )
    CREATE NONCLUSTERED INDEX [IX_SubCategories_CategoryId] ON [dbo].[SubCategories]([CategoryId] ASC)
    CREATE NONCLUSTERED INDEX [IX_SubCategories_IsActive] ON [dbo].[SubCategories]([IsActive] ASC)
    CREATE NONCLUSTERED INDEX [IX_SubCategories_Name] ON [dbo].[SubCategories]([Name] ASC)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_SubCategories_Slug] ON [dbo].[SubCategories]([Slug] ASC)
    ALTER TABLE [dbo].[SubCategories] ADD DEFAULT ((1)) FOR [IsActive]
    ALTER TABLE [dbo].[SubCategories] ADD DEFAULT (getdate()) FOR [CreatedAt]
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
        PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_Products_Categories] FOREIGN KEY([CategoryId]) REFERENCES [dbo].[Categories]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_Products_SubCategories] FOREIGN KEY([SubCategoryId]) REFERENCES [dbo].[SubCategories]([Id]) ON DELETE CASCADE
    )
    CREATE NONCLUSTERED INDEX [IX_Products_CategoryId] ON [dbo].[Products]([CategoryId] ASC)
    CREATE NONCLUSTERED INDEX [IX_Products_SubCategoryId] ON [dbo].[Products]([SubCategoryId] ASC)
    CREATE NONCLUSTERED INDEX [IX_Products_IsActive] ON [dbo].[Products]([IsActive] ASC)
    CREATE NONCLUSTERED INDEX [IX_Products_Name] ON [dbo].[Products]([Name] ASC)
    CREATE UNIQUE NONCLUSTERED INDEX [IX_Products_Slug] ON [dbo].[Products]([Slug] ASC)
    ALTER TABLE [dbo].[Products] ADD DEFAULT ((1)) FOR [IsActive]
    ALTER TABLE [dbo].[Products] ADD DEFAULT ((0)) FOR [Stock]
    ALTER TABLE [dbo].[Products] ADD DEFAULT (getdate()) FOR [CreatedAt]
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
