/**
 * Favori ve Karşılaştırma Listesi Yönetimi
 * Local Storage'de verileri tutar
 */

class FavoritesAndCompare {
    constructor() {
        this.favoritesKey = 'balonpark_favorites';
        this.compareKey = 'balonpark_compare';
        this.maxCompareItems = 4; // Maksimum 4 ürün karşılaştırılabilir
        
        this.init();
    }

    init() {
        this.loadFromStorage();
        this.updateUI();
        this.bindEvents();
    }

    // Local Storage'dan verileri yükle
    loadFromStorage() {
        this.favorites = JSON.parse(localStorage.getItem(this.favoritesKey) || '[]');
        this.compareList = JSON.parse(localStorage.getItem(this.compareKey) || '[]');
    }

    // Local Storage'a verileri kaydet
    saveToStorage() {
        localStorage.setItem(this.favoritesKey, JSON.stringify(this.favorites));
        localStorage.setItem(this.compareKey, JSON.stringify(this.compareList));
    }

    // Favori ekle/çıkar
    toggleFavorite(productId, productData) {
        const index = this.favorites.findIndex(item => item.id === productId);
        
        if (index > -1) {
            // Favorilerden çıkar
            this.favorites.splice(index, 1);
            this.showNotification('Ürün favorilerden çıkarıldı', 'info');
        } else {
            // Favorilere ekle
            this.favorites.push({
                id: productId,
                name: productData.name,
                price: productData.price,
                image: productData.image,
                url: productData.url,
                addedAt: new Date().toISOString()
            });
            this.showNotification('Ürün favorilere eklendi', 'success');
        }
        
        this.saveToStorage();
        this.updateUI();
    }

    // Karşılaştırmaya ekle/çıkar
    toggleCompare(productId, productData) {
        const index = this.compareList.findIndex(item => item.id === productId);
        
        if (index > -1) {
            // Karşılaştırmadan çıkar
            this.compareList.splice(index, 1);
            this.showNotification('Ürün karşılaştırmadan çıkarıldı', 'info');
        } else {
            // Karşılaştırmaya ekle
            if (this.compareList.length >= this.maxCompareItems) {
                this.showNotification(`En fazla ${this.maxCompareItems} ürün karşılaştırılabilir`, 'warning');
                return;
            }
            
            // Slug'ı URL'den veya data attribute'dan al
            const slug = this.getProductSlug(productData.url);
            
            this.compareList.push({
                id: productId,
                name: productData.name,
                price: productData.price,
                image: productData.image,
                url: productData.url,
                slug: slug,
                addedAt: new Date().toISOString()
            });
            this.showNotification('Ürün karşılaştırmaya eklendi', 'success');
        }
        
        this.saveToStorage();
        this.updateUI();
    }

    // URL'den slug çıkar
    getProductSlug(url) {
        try {
            const urlObj = new URL(url);
            const parts = urlObj.pathname.split('/').filter(p => p);
            // URL formatı: /category/{categorySlug}/{subCategorySlug}/{productSlug}
            return parts[parts.length - 1] || '';
        } catch {
            return '';
        }
    }

    // Slug'a göre karşılaştırmadan kaldır
    removeFromCompareBySlug(slug) {
        const index = this.compareList.findIndex(item => item.slug === slug);
        if (index > -1) {
            this.compareList.splice(index, 1);
            this.saveToStorage();
            this.updateUI();
        }
    }

    // Ürünün favori olup olmadığını kontrol et
    isFavorite(productId) {
        return this.favorites.some(item => item.id === productId);
    }

    // Ürünün karşılaştırmada olup olmadığını kontrol et
    isInCompare(productId) {
        return this.compareList.some(item => item.id === productId);
    }

    // UI'ı güncelle
    updateUI() {
        // Tüm favori butonlarını güncelle (hem .favorite-btn hem .favorite-btn-detail)
        document.querySelectorAll('.favorite-btn, .favorite-btn-detail').forEach(btn => {
            const productId = parseInt(btn.dataset.productId);
            const isFav = this.isFavorite(productId);
            
            const icon = btn.querySelector('i');
            const text = btn.querySelector('.btn-text');
            
            if (isFav) {
                btn.classList.add('active');
                if (icon) icon.className = 'fa-solid fa-heart';
                if (text) text.textContent = 'Favorilerden Çıkar';
            } else {
                btn.classList.remove('active');
                if (icon) icon.className = 'fa-regular fa-heart';
                if (text) text.textContent = 'Favorilere Ekle';
            }
        });

        // Tüm karşılaştırma butonlarını güncelle (hem .compare-btn hem .compare-btn-detail)
        document.querySelectorAll('.compare-btn, .compare-btn-detail').forEach(btn => {
            const productId = parseInt(btn.dataset.productId);
            const isInCompare = this.isInCompare(productId);
            const isMaxReached = this.compareList.length >= this.maxCompareItems && !isInCompare;
            
            if (isMaxReached) {
                btn.disabled = true;
                btn.title = `En fazla ${this.maxCompareItems} ürün karşılaştırılabilir`;
            } else {
                btn.disabled = false;
                btn.title = isInCompare ? 'Karşılaştırmadan Çıkar' : 'Karşılaştırmaya Ekle';
            }
            
            const icon = btn.querySelector('i');
            const text = btn.querySelector('.btn-text');
            
            if (isInCompare) {
                btn.classList.add('active');
                if (icon) icon.className = 'fa-solid fa-shuffle';
                if (text) text.textContent = 'Karşılaştırmadan Çıkar';
            } else {
                btn.classList.remove('active');
                if (icon) icon.className = 'fa-solid fa-shuffle';
                if (text) text.textContent = 'Karşılaştırmaya Ekle';
            }
        });

        // Header'daki sayaçları güncelle
        this.updateHeaderCounters();
    }

    // Header'daki sayaçları güncelle
    updateHeaderCounters() {
        // Eski badge sayaçları (eğer varsa)
        const favoritesCount = document.querySelector('.favorites-count');
        const compareCount = document.querySelector('.compare-count');
        
        if (favoritesCount) {
            favoritesCount.textContent = this.favorites.length;
            favoritesCount.style.display = this.favorites.length > 0 ? 'inline' : 'none';
        }
        
        if (compareCount) {
            compareCount.textContent = this.compareList.length;
            compareCount.style.display = this.compareList.length > 0 ? 'inline' : 'none';
        }

        // Yeni contact-box stili sayaçları
        const favoritesCountText = document.querySelector('.favorites-count-text');
        const compareCountText = document.querySelector('.compare-count-text');
        
        if (favoritesCountText) {
            favoritesCountText.textContent = `${this.favorites.length} Ürün`;
        }
        
        if (compareCountText) {
            compareCountText.textContent = `${this.compareList.length} Ürün`;
        }
    }

    // Event'leri bağla
    bindEvents() {
        // Favori butonları (hem .favorite-btn hem .favorite-btn-detail)
        document.addEventListener('click', (e) => {
            if (e.target.closest('.favorite-btn') || e.target.closest('.favorite-btn-detail')) {
                e.preventDefault();
                const btn = e.target.closest('.favorite-btn, .favorite-btn-detail');
                const productId = parseInt(btn.dataset.productId);
                const productData = this.getProductData(btn);
                this.toggleFavorite(productId, productData);
            }
        });

        // Karşılaştırma butonları (hem .compare-btn hem .compare-btn-detail)
        document.addEventListener('click', (e) => {
            if (e.target.closest('.compare-btn') || e.target.closest('.compare-btn-detail')) {
                e.preventDefault();
                const btn = e.target.closest('.compare-btn, .compare-btn-detail');
                const productId = parseInt(btn.dataset.productId);
                const productData = this.getProductData(btn);
                this.toggleCompare(productId, productData);
            }
        });
    }

    // Ürün verilerini al
    getProductData(btn) {
        // Önce ürün kartını bul (liste veya detay sayfası)
        const card = btn.closest('.product-box, .product-card, .modern-product-card, .product-info');
        
        // Başlık/isim için birden fazla seçici dene
        const name = card?.querySelector('.name, .product-title, h1.product-title')?.textContent?.trim() || '';
        
        // Fiyat için aktif fiyatı al
        const priceElement = card?.querySelector('.price.active, .price-main.active, .price');
        const price = priceElement?.textContent?.trim() || '';
        
        // Resim için
        const image = card?.querySelector('img')?.src || '';
        
        // URL için önce link bul, yoksa mevcut sayfayı kullan
        const url = card?.querySelector('a')?.href || window.location.href;
        
        return { name, price, image, url };
    }

    // Bildirim göster
    showNotification(message, type = 'info') {
        // Mevcut bildirimleri temizle
        const existing = document.querySelector('.favorites-notification');
        if (existing) existing.remove();

        // Yeni bildirim oluştur
        const notification = document.createElement('div');
        notification.className = `favorites-notification favorites-notification-${type}`;
        notification.innerHTML = `
            <div class="notification-content">
                <i class="fa-solid fa-${type === 'success' ? 'check' : type === 'warning' ? 'exclamation' : 'info'}"></i>
                <span>${message}</span>
            </div>
        `;

        // Stil ekle
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            background: ${type === 'success' ? '#10b981' : type === 'warning' ? '#f59e0b' : '#3b82f6'};
            color: white;
            padding: 12px 16px;
            border-radius: 8px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.15);
            z-index: 9999;
            font-size: 14px;
            font-weight: 500;
            max-width: 300px;
            transform: translateX(100%);
            transition: transform 0.3s ease;
        `;

        document.body.appendChild(notification);

        // Animasyon
        setTimeout(() => {
            notification.style.transform = 'translateX(0)';
        }, 100);

        // 3 saniye sonra kaldır
        setTimeout(() => {
            notification.style.transform = 'translateX(100%)';
            setTimeout(() => notification.remove(), 300);
        }, 3000);
    }

    // Favori listesini al
    getFavorites() {
        return this.favorites;
    }

    // Karşılaştırma listesini al
    getCompareList() {
        return this.compareList;
    }

    // Favorileri temizle
    clearFavorites() {
        this.favorites = [];
        this.saveToStorage();
        this.updateUI();
    }

    // Karşılaştırmayı temizle
    clearCompare() {
        this.compareList = [];
        this.saveToStorage();
        this.updateUI();
    }

    // Karşılaştırma URL'i oluştur
    getCompareUrl() {
        const slugs = this.compareList.map(p => p.slug).filter(s => s).join(':');
        return slugs ? `/Compare/${slugs}` : '/Compare';
    }

    // URL'i localStorage ile senkronize et
    syncUrlWithStorage() {
        // Eğer Compare sayfasındaysak, URL'den slug'ları al ve localStorage'ı güncelle
        if (window.location.pathname.startsWith('/Compare/')) {
            const pathParts = window.location.pathname.split('/');
            const slugsParam = pathParts[2]; // Compare/slugs:slugs:slugs
            
            if (slugsParam) {
                const slugs = slugsParam.split(':');
                
                // Mevcut compareList'teki slug'ları kontrol et
                const currentSlugs = this.compareList.map(p => p.slug);
                
                // Eğer farklıysa, URL'deki slug'ları kullan
                if (JSON.stringify(currentSlugs.sort()) !== JSON.stringify(slugs.sort())) {
                    // URL'deki slug'lar öncelikli
                    console.log('URL\'den slug\'lar alındı:', slugs);
                }
            }
        }
    }

    // Favorileri göster (SweetAlert2 ile)
    async showFavorites() {
        if (this.favorites.length === 0) {
            Swal.fire({
                icon: 'info',
                title: 'Favori Listeniz Boş',
                text: 'Henüz favori ürün eklememişsiniz.',
                confirmButtonText: 'Tamam',
                confirmButtonColor: '#6262a6'
            });
            return;
        }

        const productsHtml = this.favorites.map(product => `
            <div class="compare-product-card">
                <img src="${product.image}" alt="${product.name}" class="compare-product-img">
                <div class="compare-product-info">
                    <h4>${product.name}</h4>
                    <p class="compare-product-price">${product.price}</p>
                    <a href="${product.url}" class="btn btn-sm btn-primary">Ürünü Gör</a>
                    <button onclick="window.favoritesAndCompare.toggleFavorite(${product.id}, ${JSON.stringify(product).replace(/"/g, '&quot;')})" class="btn btn-sm btn-danger mt-2">Kaldır</button>
                </div>
            </div>
        `).join('');

        Swal.fire({
            title: `Favorilerim (${this.favorites.length})`,
            html: `<div class="compare-products-grid">${productsHtml}</div>`,
            width: '800px',
            showCloseButton: true,
            showConfirmButton: false,
            customClass: {
                popup: 'compare-popup'
            }
        });
    }

    // Karşılaştırma sayfasına yönlendir
    async showCompare() {
        if (this.compareList.length === 0) {
            Swal.fire({
                icon: 'info',
                title: 'Karşılaştırma Listeniz Boş',
                text: 'Henüz karşılaştırmak için ürün eklememişsiniz.',
                confirmButtonText: 'Tamam',
                confirmButtonColor: '#6262a6'
            });
            return;
        }

        // Slug'ları : ile birleştir
        const slugs = this.compareList.map(p => p.slug).filter(s => s).join(':');
        
        if (!slugs) {
            Swal.fire({
                icon: 'error',
                title: 'Hata',
                text: 'Ürün bilgileri eksik. Lütfen tekrar deneyin.',
                confirmButtonText: 'Tamam',
                confirmButtonColor: '#6262a6'
            });
            return;
        }

        // Karşılaştırma sayfasına yönlendir
        window.location.href = `/Compare/${slugs}`;
    }

    // Karşılaştırma tablosunu oluştur
    buildCompareTable(products) {
        const fields = [
            { key: 'image', label: 'Görsel', type: 'image' },
            { key: 'name', label: 'Ürün Adı', type: 'text' },
            { key: 'productCode', label: 'Ürün Kodu', type: 'text' },
            { key: 'categoryName', label: 'Kategori', type: 'text' },
            { key: 'subCategoryName', label: 'Alt Kategori', type: 'text' },
            { key: 'price', label: 'Fiyat (TL)', type: 'price' },
            { key: 'usdPrice', label: 'Fiyat (USD)', type: 'price' },
            { key: 'euroPrice', label: 'Fiyat (EUR)', type: 'price' },
            { key: 'dimensions', label: 'Boyutlar', type: 'text' },
            { key: 'stock', label: 'Stok', type: 'number' },
            { key: 'description', label: 'Açıklama', type: 'html' },
            { key: 'technicalDescription', label: 'Teknik Açıklama', type: 'html' },
            { key: 'actions', label: 'İşlemler', type: 'actions' }
        ];

        let html = '<div class="compare-table-container"><table class="compare-table"><tbody>';

        fields.forEach(field => {
            html += '<tr>';
            html += `<th>${field.label}</th>`;
            
            products.forEach(product => {
                html += '<td>';
                
                switch (field.type) {
                    case 'image':
                        html += `<img src="${product.imageUrl}" alt="${product.name}" style="width: 100px; height: 100px; object-fit: cover; border-radius: 8px;">`;
                        break;
                    case 'price':
                        const value = product[field.key];
                        html += `<strong>${value ? value.toFixed(2) : '0.00'}</strong>`;
                        break;
                    case 'number':
                        html += product[field.key] || '0';
                        break;
                    case 'html':
                        const content = product[field.key] || '-';
                        html += `<div style="max-height: 150px; overflow-y: auto;">${content}</div>`;
                        break;
                    case 'actions':
                        html += `
                            <a href="/category/${product.categorySlug}/${product.subCategorySlug}/${product.slug}" class="btn btn-sm btn-primary mb-2" style="width: 100%;">Ürünü Gör</a>
                            <button onclick="window.favoritesAndCompare.toggleCompare(${product.id}, {})" class="btn btn-sm btn-danger" style="width: 100%;">Kaldır</button>
                        `;
                        break;
                    default:
                        html += product[field.key] || '-';
                }
                
                html += '</td>';
            });
            
            html += '</tr>';
        });

        html += '</tbody></table></div>';
        return html;
    }
}

// Global instance oluştur
window.favoritesAndCompare = new FavoritesAndCompare();

// Global fonksiyonlar (showFavorites ve showCompare)
window.showFavorites = function() {
    window.favoritesAndCompare.showFavorites();
};

window.showCompare = function() {
    window.favoritesAndCompare.showCompare();
};

// Sayfa yüklendiğinde çalıştır
document.addEventListener('DOMContentLoaded', function() {
    // Eğer henüz oluşturulmamışsa oluştur
    if (!window.favoritesAndCompare) {
        window.favoritesAndCompare = new FavoritesAndCompare();
    }
});
