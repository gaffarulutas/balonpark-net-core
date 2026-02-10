-- Products Tablosuna DisplayOrder Kolonu Ekleme

-- 1. Products tablosuna DisplayOrder kolonu ekle
ALTER TABLE Products
ADD DisplayOrder INT NOT NULL DEFAULT 0;

-- 2. Mevcut ürünler için DisplayOrder değerlerini ayarla (Id'ye göre)
UPDATE Products
SET DisplayOrder = Id;

-- 3. Index oluştur (performans için)
CREATE INDEX IX_Products_DisplayOrder ON Products(DisplayOrder);

-- 4. Kontrol et
SELECT Id, Name, CategoryId, SubCategoryId, DisplayOrder 
FROM Products 
ORDER BY CategoryId, SubCategoryId, DisplayOrder;

