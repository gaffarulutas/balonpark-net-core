# Balon Park E-Commerce Project

ASP.NET Core 8.0 ile geliştirilmiş modern bir e-ticaret projesi.

## 🚀 Özellikler

- **Modern UI/UX**: Responsive ve kullanıcı dostu arayüz
- **Admin Panel**: Kategori, alt kategori ve ürün yönetimi
- **Ürün Yönetimi**: Çoklu resim yükleme, otomatik thumbnail oluşturma
- **Veritabanı**: MS SQL Server ile Dapper ORM
- **Session Yönetimi**: Güvenli oturum yönetimi
- **Resim İşleme**: Otomatik boyutlandırma (original, large, thumbnail)

## 📋 Teknolojiler

- **Backend**: ASP.NET Core 8.0 (Razor Pages)
- **Database**: MS SQL Server
- **ORM**: Dapper
- **Frontend**: HTML5, CSS3, JavaScript, Bootstrap 5
- **Image Processing**: ImageSharp

## 🛠️ Kurulum

### Gereksinimler

- .NET 8.0 SDK
- MS SQL Server
- Visual Studio 2022 veya VS Code

### Adımlar

1. **Projeyi klonlayın**
```bash
git clone https://github.com/gaffarulutas/balonpark-net-core.git
cd balonpark-net-core
```

2. **Veritabanını oluşturun**
```sql
-- MS SQL Server'da veritabanını oluşturun
CREATE DATABASE BalonParkDb;
GO

-- DATABASE_SCRIPT.sql dosyasını çalıştırın
```

3. **Connection String'i güncelleyin**
`BalonPark/appsettings.json` dosyasında:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=BalonParkDb;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

4. **Projeyi çalıştırın**
```bash
cd BalonPark
dotnet run
```

5. **Tarayıcıda açın**
- Ana Sayfa: `https://localhost:5001`
- Admin Panel: `https://localhost:5001/Admin`

## 📁 Proje Yapısı

```
BalonPark/
├── Data/                   # Repository sınıfları
├── Models/                 # Veri modelleri
├── Helpers/                # Yardımcı sınıflar
├── Pages/                  # Razor Pages
│   ├── Admin/             # Admin paneli sayfaları
│   └── Shared/            # Paylaşılan layout'lar
├── wwwroot/               # Statik dosyalar
│   ├── assets/            # CSS, JS, resimler
│   └── uploads/           # Yüklenen dosyalar
└── appsettings.json       # Konfigürasyon
```

## 👤 Admin Girişi

Admin paneline erişim için:
- **URL**: `/Admin/Login`
- **Kullanıcı Adı**: admin
- **Şifre**: admin123

## 📝 Özellikler

### Admin Panel
- ✅ Kategori yönetimi (CRUD)
- ✅ Alt kategori yönetimi (CRUD)
- ✅ Ürün yönetimi (CRUD)
- ✅ Çoklu resim yükleme
- ✅ Ana resim seçimi
- ✅ Otomatik thumbnail oluşturma

### Ana Sayfa
- ✅ Dinamik kategori listesi
- ✅ Dinamik ürün listesi
- ✅ Modern slider/banner
- ✅ Responsive tasarım
- ✅ WOW.js animasyonlar

## 🔐 Güvenlik

- Session tabanlı authentication
- SQL Injection koruması (Dapper parametreli sorgular)
- XSS koruması
- Güvenli dosya yükleme

## 📄 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

## 📞 İletişim

- GitHub: [@gaffarulutas](https://github.com/gaffarulutas)
- Proje: [balonpark-net-core](https://github.com/gaffarulutas/balonpark-net-core)

---

⭐ Bu projeyi beğendiyseniz yıldız vermeyi unutmayın!
