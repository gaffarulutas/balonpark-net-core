# 📧 Email Yönetimi - Admin Panel

Admin paneline **IMAP/SMTP** tabanlı profesyonel email yönetim sistemi eklenmiştir.

## ✨ Özellikler

### 📥 Email Okuma (IMAP)
- **Gelen Kutusu (Inbox)** - Gelen mesajları görüntüleme
- **Gönderilen (Sent)** - Gönderilen mesajları görüntüleme  
- **Taslaklar (Drafts)** - Taslak mesajları yönetme
- **Spam/Junk** - Spam klasörü
- **Çöp Kutusu (Trash)** - Silinmiş mesajlar
- **Özel Klasörler** - Diğer tüm mail klasörleri

### 📤 Email Gönderme (SMTP)
- Yeni email oluşturma
- Mesajlara yanıt gönderme
- HTML/Plain Text desteği
- Ek dosya desteği (görüntüleme)

### 🎯 Email İşlemleri
- ✅ Okundu/Okunmadı işaretleme
- ⭐ Önemli olarak işaretleme
- 📂 Klasörler arası taşıma
- 🗑️ Silme (Trash'e taşıma)
- 🔍 Arama (konu, gönderen, içerik)

### 📊 İstatistikler
- Toplam mesaj sayısı
- Okunmamış mesaj sayısı
- Önemli mesajlar
- Bugün/Bu hafta istatistikleri

## 🏗️ Teknik Mimari

### Best Practices Uygulamaları

#### 1. **Connection Pooling**
```csharp
- Tek bir IMAP bağlantısı paylaşılır (5 dakika yaşam süresi)
- Gereksiz bağlantı açma/kapama önlenir
- Thread-safe SemaphoreSlim ile yönetilir
```

#### 2. **Retry Logic**
```csharp
- IMAP: 3 deneme (exponential backoff: 2, 4, 8 saniye)
- SMTP: 2 deneme (2 saniye bekleme)
- Her başarısız denemede detaylı loglama
```

#### 3. **Proper Disposal Pattern**
```csharp
- IDisposable implementation
- Bağlantılar düzgün kapatılır
- Memory leak önlenir
```

#### 4. **Error Handling**
```csharp
- Socket exception durumunda connection reset
- Her method try-catch ile korunur
- Kullanıcıya anlamlı hata mesajları
```

#### 5. **Resource Management**
```csharp
- Async/await pattern
- CancellationToken desteği
- Timeout yönetimi (30 saniye)
```

## 📁 Dosya Yapısı

```
UnluPark/
├── Models/
│   └── EmailMessage.cs          # Email modelleri
├── Services/
│   ├── IMailService.cs          # Mail service interface
│   └── MailService.cs           # IMAP/SMTP implementasyonu
└── Pages/Admin/Mails/
    ├── Index.cshtml             # Email listesi (klasörlü)
    ├── Index.cshtml.cs          
    ├── Compose.cshtml           # Yeni email/yanıt
    ├── Compose.cshtml.cs        
    ├── View.cshtml              # Email detay
    └── View.cshtml.cs           
```

## ⚙️ Konfigürasyon

`appsettings.json` ayarları:

```json
{
  "EmailSettings": {
    "SmtpServer": "srvm15.trwww.com",
    "SmtpPort": 587,
    "SmtpUsername": "info@unlupark.com",
    "SmtpPassword": "Terra2010*",
    "ImapServer": "srvm15.trwww.com",
    "ImapPort": 993,
    "ImapUsername": "info@unlupark.com",
    "ImapPassword": "Terra2010*",
    "FromEmail": "info@unlupark.com",
    "FromName": "Ünlü Park Şişme Oyun Grupları",
    "ToEmail": "info@unlupark.com",
    "EnableSsl": true
  }
}
```

### IMAP Port Seçimi
- **993** → SSL/TLS (Güvenli, önerilen) ✅
- **143** → STARTTLS (Alternatif)

### SMTP Port Seçimi
- **587** → STARTTLS (Modern, önerilen) ✅
- **465** → SSL/TLS (Eski)
- **25** → Güvensiz (kullanmayın)

## 🔒 Güvenlik

### SSL Certificate Validation
⚠️ **Development Modu:**
```csharp
ServerCertificateValidationCallback = (s, c, h, e) => true
```
Bu satır tüm sertifikaları kabul eder.

✅ **Production için:**
```csharp
// Bu satırı kaldırın veya proper validation ekleyin
ServerCertificateValidationCallback = (s, c, h, e) => 
{
    // Sertifika kontrolü yapın
    return e == SslPolicyErrors.None;
}
```

## 🚀 Kullanım

### Admin Panelden Erişim
1. `/admin` → Dashboard
2. Sol menüden **"Email Yönetimi"** 
3. Klasörler arasında geçiş yapın
4. Mesaj okuyin, yanıtlayın veya yeni email gönderin

### İletişim Formu Entegrasyonu
İletişim formundan gelen mesajlar otomatik olarak IMAP klasörünüzde görünür:
- Form submit edilir
- SMTP ile email gönderilir
- IMAP klasöründe "Gönderilmiş" olarak saklanır
- Admin panelden görüntülenebilir

## 📦 NuGet Paketleri

```xml
<PackageReference Include="MailKit" Version="4.9.0" />
<PackageReference Include="MimeKit" Version="4.9.0" />
```

## 🐛 Troubleshooting

### "Operation timed out" Hatası
- IMAP/SMTP sunucu adresini kontrol edin
- Port numaralarını doğrulayın
- Firewall ayarlarını kontrol edin
- Sunucunun erişilebilir olduğundan emin olun

### "Authentication failed" Hatası
- Username/password doğruluğunu kontrol edin
- Email hesabında "Less secure apps" ayarı gerekebilir
- 2FA etkinse "App Password" kullanın

### "Some messages no longer exist" Hatası
✅ **Düzeltildi!** UID tabanlı fetch kullanılıyor.

## 🎨 UI Özellikleri

- ✅ Responsive tasarım (mobile uyumlu)
- ✅ Tailwind CSS ile modern görünüm
- ✅ Real-time işlem geri bildirimi
- ✅ SweetAlert2 ile kullanıcı dostu uyarılar
- ✅ Loading states
- ✅ Full-width layout (rahat okuma)

## 📈 Performans Optimizasyonları

1. **Connection Pooling** - Bağlantılar tekrar kullanılır
2. **Lazy Loading** - Sayfalama ile yavaş yükleme
3. **Async Operations** - Non-blocking işlemler
4. **Caching** - 5 dakika client cache
5. **Minimal Fetch** - Sadece gerekli data çekilir

## 🔄 Gelecek Geliştirmeler

- [ ] Ek dosya indirme
- [ ] Çoklu mesaj seçimi ve toplu işlemler
- [ ] Email templates
- [ ] Otomatik yanıtlama kuralları
- [ ] Email imzası
- [ ] Klasör oluşturma/silme
- [ ] Email filtering/sorting rules

## 📝 Notlar

- Her sayfa değişiminde IMAP bağlantısı tekrar kullanılır
- Bağlantı 5 dakika boyunca açık kalır
- Timeout durumunda otomatik retry
- Tüm email işlemleri loglanır
- Production'da SSL certificate validation aktif edilmeli

---
**Son Güncelleme:** 13 Ekim 2025
**Versiyon:** 1.0.0

