-- Kategori ve Alt Kategori Sıralama İçin DisplayOrder Kolonları Ekleme
-- Bu script'i veritabanında çalıştırın

-- 1. Categories tablosuna DisplayOrder kolonu ekle
ALTER TABLE Categories 
ADD DisplayOrder INT NOT NULL DEFAULT 0;

-- 2. SubCategories tablosuna DisplayOrder kolonu ekle
ALTER TABLE SubCategories 
ADD DisplayOrder INT NOT NULL DEFAULT 0;

-- 3. Mevcut kategorilere otomatik sıralama numarası ver (Id'ye göre)
UPDATE Categories 
SET DisplayOrder = Id;

-- 4. Mevcut alt kategorilere otomatik sıralama numarası ver (Id'ye göre)
UPDATE SubCategories 
SET DisplayOrder = Id;

-- 5. Index ekle (performans için)
CREATE INDEX IX_Categories_DisplayOrder ON Categories(DisplayOrder);
CREATE INDEX IX_SubCategories_DisplayOrder ON SubCategories(DisplayOrder);

-- Kontrol sorguları
SELECT Id, Name, DisplayOrder FROM Categories ORDER BY DisplayOrder;
SELECT Id, Name, CategoryId, DisplayOrder FROM SubCategories ORDER BY CategoryId, DisplayOrder;

