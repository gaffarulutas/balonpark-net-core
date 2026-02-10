# Cache Mekanizması - Eksiksiz Dokümantasyon

## ✅ Tamamlanan İşlemler

### 1. **ProductRepository** ✅
- **Create (CreateAsync)**: Yeni ürün eklendiğinde cache temizlenir ve tüm ürünler yeniden yüklenir
- **Update (UpdateAsync)**: Ürün güncellendiğinde ilgili cache'ler temizlenir, eski slug temizlenir ve tüm ürünler yeniden yüklenir
- **Delete (DeleteAsync)**: Ürün silindiğinde cache temizlenir ve tüm ürünler yeniden yüklenir

### 2. **CategoryRepository** ✅
- **Create (CreateAsync)**: Yeni kategori eklendiğinde cache temizlenir ve tüm kategoriler yeniden yüklenir
- **Update (UpdateAsync)**: Kategori güncellendiğinde ilgili cache'ler temizlenir, eski slug temizlenir ve tüm kategoriler yeniden yüklenir
- **Delete (DeleteAsync)**: Kategori silindiğinde cache temizlenir ve tüm kategoriler yeniden yüklenir

### 3. **SubCategoryRepository** ✅
- **Create (CreateAsync)**: Yeni alt kategori eklendiğinde cache temizlenir ve tüm alt kategoriler yeniden yüklenir
- **Update (UpdateAsync)**: Alt kategori güncellendiğinde ilgili cache'ler temizlenir, eski slug temizlenir ve tüm alt kategoriler yeniden yüklenir
- **Delete (DeleteAsync)**: Alt kategori silindiğinde cache temizlenir ve tüm alt kategoriler yeniden yüklenir

### 4. **BlogRepository** ✅
- **Create (CreateAsync)**: Yeni blog eklendiğinde cache temizlenir ve tüm bloglar yeniden yüklenir
- **Update (UpdateAsync)**: Blog güncellendiğinde ilgili cache'ler temizlenir, eski slug temizlenir ve tüm bloglar yeniden yüklenir
- **Delete (DeleteAsync)**: Blog silindiğinde cache temizlenir ve tüm bloglar yeniden yüklenir

### 5. **ProductImageRepository** ✅
- **Create (CreateAsync)**: Yeni resim eklendiğinde ilgili ürünün cache'i temizlenir
- **SetMainImage (SetMainImageAsync)**: Ana resim değiştirildiğinde ilgili ürünün cache'i temizlenir
- **Delete (DeleteAsync)**: Resim silindiğinde ilgili ürünün cache'i temizlenir

## 📊 Cache Stratejisi

### Okuma İşlemleri (Read):
1. İlk önce cache'den veri kontrol edilir
2. Cache'de veri varsa direkt döndürülür (hızlı)
3. Cache'de veri yoksa veritabanından çekilir
4. Veritabanından çekilen veri cache'e kaydedilir
5. Veri kullanıcıya döndürülür

### Yazma İşlemleri (Create/Update/Delete):
1. Veritabanında işlem yapılır
2. İşlem başarılıysa ilgili cache'ler temizlenir
3. Veritabanından güncel veriler çekilir
4. Güncel veriler cache'e kaydedilir
5. Böylece bir sonraki okuma işleminde cache güncel olur

## 🎯 Faydaları

1. **Performans**: Veritabanı sorguları azalır, veriler bellekten okunur
2. **Tutarlılık**: Her değişiklikte cache otomatik yenilenir
3. **Güvenilirlik**: Cache her zaman güncel veriyi içerir
4. **Ölçeklenebilirlik**: Yüksek trafik altında daha iyi performans

## ⚙️ Cache Süresi

- Varsayılan cache süresi: **30 dakika**
- CacheService.cs dosyasında tanımlı: `_cacheExpiration = TimeSpan.FromMinutes(30)`
- Bu süre geçtikten sonra cache otomatik temizlenir

## 🔄 Otomatik Cache Yenileme

Her CRUD işleminde:
- ✅ Cache temizlenir (InvalidateAsync)
- ✅ Veritabanından güncel veri çekilir (GetAll...FromDatabaseAsync)
- ✅ Güncel veri cache'e kaydedilir (SetAsync)

## 📝 Kullanım Örnekleri

### Ürün Ekleme:
```csharp
var newId = await productRepository.CreateAsync(product);
// Cache otomatik olarak yenilenir, bir sonraki okuma güncel veriyi getirir
```

### Kategori Güncelleme:
```csharp
await categoryRepository.UpdateAsync(category);
// Hem eski slug hem de yeni slug cache'den temizlenir
// Tüm kategoriler yeniden cache'e yüklenir
```

### Blog Silme:
```csharp
await blogRepository.DeleteAsync(blogId);
// Blog cache'den temizlenir
// Tüm bloglar yeniden cache'e yüklenir
```

## ✨ Sonuç

Cache mekanizması eksiksiz olarak tamamlanmıştır. Her ekleme, güncelleme ve silme işleminde cache otomatik olarak yenilenir ve tutarlı kalır.
