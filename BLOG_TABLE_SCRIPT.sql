-- Blog tablosu oluşturma scripti
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

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
PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO

-- Indexes
CREATE NONCLUSTERED INDEX [IX_Blogs_IsActive] ON [dbo].[Blogs]
(
	[IsActive] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Blogs_IsFeatured] ON [dbo].[Blogs]
(
	[IsFeatured] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Blogs_PublishedAt] ON [dbo].[Blogs]
(
	[PublishedAt] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

CREATE NONCLUSTERED INDEX [IX_Blogs_Category] ON [dbo].[Blogs]
(
	[Category] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

SET ANSI_PADDING ON
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_Blogs_Slug] ON [dbo].[Blogs]
(
	[Slug] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
GO

-- Default values
ALTER TABLE [dbo].[Blogs] ADD  DEFAULT ((1)) FOR [IsActive]
GO
ALTER TABLE [dbo].[Blogs] ADD  DEFAULT ((0)) FOR [IsFeatured]
GO
ALTER TABLE [dbo].[Blogs] ADD  DEFAULT ((0)) FOR [ViewCount]
GO
ALTER TABLE [dbo].[Blogs] ADD  DEFAULT (getdate()) FOR [CreatedAt]
GO
ALTER TABLE [dbo].[Blogs] ADD  DEFAULT ('Ünlü Park') FOR [AuthorName]
GO

-- Örnek blog verileri
INSERT INTO [dbo].[Blogs] ([Title], [Slug], [Content], [Excerpt], [FeaturedImage], [MetaTitle], [MetaDescription], [MetaKeywords], [IsActive], [IsFeatured], [ViewCount], [CreatedAt], [PublishedAt], [AuthorName], [Category])
VALUES 
(
    'Şişme Oyun Parkları Hakkında Bilmeniz Gerekenler',
    'sisime-oyun-parklari-hakkinda-bilmeniz-gerekenler',
    '<h2>Şişme Oyun Parkları Nedir?</h2>
    <p>Şişme oyun parkları, çocukların güvenli bir şekilde eğlenebileceği, dayanıklı malzemelerden üretilen eğlence alanlarıdır. Bu parklar, çeşitli oyun elemanları ve aktivite alanları içerir.</p>
    
    <h3>Şişme Oyun Parklarının Avantajları</h3>
    <ul>
        <li><strong>Güvenlik:</strong> Yumuşak yapısı sayesinde çocuklar için güvenlidir</li>
        <li><strong>Dayanıklılık:</strong> Kaliteli malzemelerden üretilmiştir</li>
        <li><strong>Eğlence:</strong> Çocukların sosyal gelişimini destekler</li>
        <li><strong>Pratiklik:</strong> Kolay kurulum ve taşıma imkanı</li>
    </ul>
    
    <h3>Ünlü Park Kalitesi</h3>
    <p>Ünlü Park olarak, tüm şişme oyun parklarımızı en kaliteli malzemelerle üretiyoruz. Ürünlerimiz CE sertifikalı olup, çocukların güvenliği için gerekli tüm standartları karşılamaktadır.</p>',
    'Şişme oyun parkları hakkında detaylı bilgi edinin. Güvenlik, dayanıklılık ve eğlence faktörlerini keşfedin.',
    '/assets/images/blog/sisime-oyun-parklari.jpg',
    'Şişme Oyun Parkları Hakkında Bilmeniz Gerekenler | Ünlü Park',
    'Şişme oyun parkları hakkında detaylı bilgi. Güvenli, dayanıklı ve eğlenceli şişme oyun parkları için Ünlü Park''ı tercih edin.',
    'şişme oyun parkı, çocuk oyun alanı, güvenli oyun parkı, şişme park, çocuk eğlence alanı',
    1,
    1,
    0,
    GETDATE(),
    GETDATE(),
    'Ünlü Park',
    'Şişme Oyun Parkları'
),
(
    'Çocuklar İçin En İyi Şişme Kaydırak Seçenekleri',
    'cocuklar-icin-en-iyi-sisime-kaydirak-secenekleri',
    '<h2>Şişme Kaydırak Çeşitleri</h2>
    <p>Çocuklar için en popüler eğlence araçlarından biri olan şişme kaydıraklar, farklı boyut ve tasarımlarla sunulmaktadır.</p>
    
    <h3>Kaydırak Seçiminde Dikkat Edilecek Noktalar</h3>
    <ul>
        <li><strong>Boyut:</strong> Çocuğun yaşına uygun boyut seçimi</li>
        <li><strong>Malzeme:</strong> Dayanıklı ve güvenli malzeme kullanımı</li>
        <li><strong>Güvenlik:</strong> CE sertifikası ve güvenlik standartları</li>
        <li><strong>Kurulum:</strong> Kolay kurulum ve bakım</li>
    </ul>
    
    <h3>En Popüler Kaydırak Modelleri</h3>
    <p>Ünlü Park''ın sunduğu kaydırak modelleri arasında:</p>
    <ul>
        <li>Klasik tek kaydıraklı modeller</li>
        <li>Çoklu kaydıraklı kompleks modeller</li>
        <li>Tema bazlı özel tasarım kaydıraklar</li>
        <li>Kompakt ev tipi kaydıraklar</li>
    </ul>',
    'Çocuklar için en iyi şişme kaydırak seçenekleri ve seçim kriterleri hakkında detaylı rehber.',
    '/assets/images/blog/sisime-kaydirak.jpg',
    'Çocuklar İçin En İyi Şişme Kaydırak Seçenekleri | Ünlü Park',
    'Çocuklar için en uygun şişme kaydırak seçimi. Güvenli, dayanıklı ve eğlenceli kaydırak modelleri için Ünlü Park.',
    'şişme kaydırak, çocuk kaydırak, güvenli kaydırak, şişme oyun parkı, çocuk eğlence',
    1,
    1,
    0,
    GETDATE(),
    GETDATE(),
    'Ünlü Park',
    'Şişme Kaydıraklar'
),
(
    'Şişme Havuzların Bakımı ve Temizliği',
    'sisime-havuzlarin-bakimi-ve-temizligi',
    '<h2>Şişme Havuz Bakım Rehberi</h2>
    <p>Şişme havuzların uzun ömürlü olması ve güvenli kullanımı için düzenli bakım ve temizlik şarttır.</p>
    
    <h3>Günlük Bakım</h3>
    <ul>
        <li>Havuz suyunun değiştirilmesi</li>
        <li>Havuz iç yüzeyinin temizlenmesi</li>
        <li>Dış yüzey kontrolü</li>
        <li>Hava basıncı kontrolü</li>
    </ul>
    
    <h3>Haftalık Bakım</h3>
    <ul>
        <li>Detaylı temizlik işlemi</li>
        <li>Havuz boşaltılıp kurutulması</li>
        <li>Malzeme kontrolü</li>
        <li>Gerekirse tamir işlemleri</li>
    </ul>
    
    <h3>Temizlik İpuçları</h3>
    <p>Havuz temizliğinde kullanabileceğiniz malzemeler:</p>
    <ul>
        <li>Yumuşak fırça ve sünger</li>
        <li>Çocuk dostu temizlik ürünleri</li>
        <li>Ilık su</li>
        <li>Havlu ve bez</li>
    </ul>',
    'Şişme havuzların bakımı ve temizliği hakkında detaylı rehber. Havuzunuzun uzun ömürlü olması için ipuçları.',
    '/assets/images/blog/sisime-havuz-bakim.jpg',
    'Şişme Havuzların Bakımı ve Temizliği | Ünlü Park',
    'Şişme havuz bakım rehberi. Havuzunuzun temiz ve güvenli kalması için bakım ipuçları ve temizlik yöntemleri.',
    'şişme havuz bakımı, havuz temizliği, şişme havuz, çocuk havuzu, havuz bakım ipuçları',
    1,
    0,
    0,
    GETDATE(),
    GETDATE(),
    'Ünlü Park',
    'Bakım ve Temizlik'
),
(
    'İç Mekan Oyun Parkları ve Avantajları',
    'ic-mekan-oyun-parklari-ve-avantajlari',
    '<h2>İç Mekan Oyun Parkları</h2>
    <p>Hava koşullarından bağımsız olarak çocukların eğlenebileceği iç mekan oyun parkları, özellikle kapalı alanlarda ideal çözümler sunar.</p>
    
    <h3>İç Mekan Parklarının Avantajları</h3>
    <ul>
        <li><strong>Hava Bağımsızlığı:</strong> Her mevsim kullanım imkanı</li>
        <li><strong>Güvenlik:</strong> Kontrollü ortam</li>
        <li><strong>Hijyen:</strong> Temiz ve bakımlı alan</li>
        <li><strong>Eğitici:</strong> Öğretici oyun elemanları</li>
    </ul>
    
    <h3>Popüler İç Mekan Oyun Elemanları</h3>
    <ul>
        <li>Softplay oyun alanları</li>
        <li>Top havuzları</li>
        <li>Labirent sistemleri</li>
        <li>Eğitici oyun köşeleri</li>
        <li>Küçük kaydıraklar</li>
    </ul>
    
    <h3>Ünlü Park İç Mekan Çözümleri</h3>
    <p>Ünlü Park olarak, iç mekan oyun parkları için özel tasarımlar sunuyoruz. Tüm ürünlerimiz, kapalı alan kullanımına uygun malzemelerle üretilmektedir.</p>',
    'İç mekan oyun parkları ve avantajları hakkında bilgi edinin. Kapalı alanlar için ideal oyun çözümleri.',
    '/assets/images/blog/ic-mekan-oyun-parki.jpg',
    'İç Mekan Oyun Parkları ve Avantajları | Ünlü Park',
    'İç mekan oyun parkları hakkında detaylı bilgi. Hava bağımsız, güvenli ve eğitici oyun alanları için Ünlü Park.',
    'iç mekan oyun parkı, kapalı oyun alanı, softplay, top havuzu, çocuk oyun alanı',
    1,
    1,
    0,
    GETDATE(),
    GETDATE(),
    'Ünlü Park',
    'İç Mekan Oyun Parkları'
),
(
    'Şişme Oyun Parkı Seçim Rehberi',
    'sisime-oyun-parki-secim-rehberi',
    '<h2>Doğru Şişme Oyun Parkı Nasıl Seçilir?</h2>
    <p>Çocuklarınız için en uygun şişme oyun parkını seçerken dikkat edilmesi gereken önemli faktörler vardır.</p>
    
    <h3>Seçim Kriterleri</h3>
    <ul>
        <li><strong>Yaş Grubu:</strong> Çocuğun yaşına uygun tasarım</li>
        <li><strong>Alan Boyutu:</strong> Kurulacak alanın büyüklüğü</li>
        <li><strong>Bütçe:</strong> Uygun fiyat aralığı</li>
        <li><strong>Kullanım Amacı:</strong> Ev, okul veya ticari kullanım</li>
    </ul>
    
    <h3>Malzeme Kalitesi</h3>
    <p>Kaliteli malzemeler şunları garanti eder:</p>
    <ul>
        <li>Uzun ömür</li>
        <li>Güvenlik</li>
        <li>Kolay bakım</li>
        <li>Dayanıklılık</li>
    </ul>
    
    <h3>Ünlü Park''tan Satın Alma Avantajları</h3>
    <ul>
        <li>CE sertifikalı ürünler</li>
        <li>2 yıl garanti</li>
        <li>Ücretsiz kargo</li>
        <li>Uzman danışmanlık</li>
        <li>Hızlı teslimat</li>
    </ul>',
    'Şişme oyun parkı seçimi için kapsamlı rehber. Doğru seçim yapmak için dikkat edilmesi gereken faktörler.',
    '/assets/images/blog/oyun-parki-secim.jpg',
    'Şişme Oyun Parkı Seçim Rehberi | Ünlü Park',
    'Şişme oyun parkı seçim rehberi. Çocuğunuz için en uygun oyun parkını seçmek için dikkat edilmesi gereken kriterler.',
    'şişme oyun parkı seçimi, çocuk oyun parkı, oyun parkı rehberi, şişme park seçimi',
    1,
    0,
    0,
    GETDATE(),
    GETDATE(),
    'Ünlü Park',
    'Seçim Rehberi'
);
