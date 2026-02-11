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

    // Local Storage'dan verileri yükle (hatalı veriye karşı dayanıklı)
    loadFromStorage() {
        try {
            this.favorites = JSON.parse(localStorage.getItem(this.favoritesKey) || '[]');
            if (!Array.isArray(this.favorites)) this.favorites = [];
        } catch {
            this.favorites = [];
        }
        try {
            this.compareList = JSON.parse(localStorage.getItem(this.compareKey) || '[]');
            if (!Array.isArray(this.compareList)) this.compareList = [];
        } catch {
            this.compareList = [];
        }
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

    // Bildirim göster (yalnızca Snackbar – toast kullanılmaz)
    showNotification(message, type = 'info') {
        const colors = { success: '#059669', error: '#dc2626', warning: '#d97706', info: '#2563eb' };
        const bg = colors[type] || colors.info;

        if (typeof showSnackbar === 'function') {
            showSnackbar(message, type);
            return;
        }
        if (typeof Snackbar !== 'undefined') {
            Snackbar.show({
                text: message,
                pos: 'bottom-right',
                duration: 5000,
                backgroundColor: bg
            });
            return;
        }

        // Fallback: Snackbar kütüphanesi yoksa snackbar tarzı DOM bildirimi
        this.showSnackbarFallback(message, bg);
    }

    // Snackbar kütüphanesi yoksa kullanılan snackbar tarzı fallback (toast değil)
    showSnackbarFallback(message, backgroundColor) {
        var wrap = document.createElement('div');
        wrap.setAttribute('role', 'status');
        wrap.setAttribute('aria-live', 'polite');
        wrap.className = 'snackbar-fallback';
        wrap.style.backgroundColor = backgroundColor;
        wrap.textContent = message;

        document.body.appendChild(wrap);
        var duration = 5000;
        var t = setTimeout(function () {
            wrap.classList.add('snackbar-fallback-out');
            setTimeout(function () { wrap.remove(); }, 260);
        }, duration);
        wrap.addEventListener('click', function () {
            clearTimeout(t);
            wrap.classList.add('snackbar-fallback-out');
            setTimeout(function () { wrap.remove(); }, 260);
        });
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

    // URL'i localStorage ile senkronize et (Compare sayfasında header sayacı doğru görünsün)
    syncUrlWithStorage() {
        if (!window.location.pathname.startsWith('/Compare/')) return;
        const pathParts = window.location.pathname.split('/').filter(Boolean);
        const slugsParam = pathParts[1]; // Compare sonrası ilk segment (slugs:slugs:slugs)
        if (!slugsParam) return;
        const slugs = slugsParam.split(':').filter(function(s) { return s && s.length > 0; });
        if (slugs.length === 0) return;
        var currentSlugs = this.compareList.map(function(p) { return p.slug; }).filter(function(s) { return s; });
        var same = currentSlugs.length === slugs.length && slugs.every(function(s, i) { return currentSlugs[i] === s; });
        if (same) return;
        // URL'deki slug'ları compareList'e yaz (id/name/url boş kalabilir; sayfa zaten sunucudan dolduruyor)
        this.compareList = slugs.map(function(slug) { return { slug: slug, id: null, name: '', price: '', image: '', url: '', addedAt: new Date().toISOString() }; });
        this.saveToStorage();
        this.updateUI();
    }

    // Favorileri göster (generic Tailwind modal – focus trap, Escape, backdrop, aria)
    showFavorites() {
        if (this.favorites.length === 0) {
            this.showNotification('Henüz favori ürün eklememişsiniz.', 'info');
            return;
        }

        const escHtml = (s) => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
        const safeUrl = (s) => {
            const u = String(s ?? '').trim();
            if (!u || u === '#') return '#';
            if (/^\s*javascript:/i.test(u)) return '#';
            return u.replace(/"/g, '&quot;');
        };
        const escProduct = (p) => ({
            name: escHtml(p.name),
            price: escHtml(p.price != null ? String(p.price) : ''),
            image: safeUrl(p.image),
            url: safeUrl(p.url)
        });

        const contentHtml = '<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">' +
            this.favorites.map(function (item) {
                const d = escProduct(item);
                return '<div class="favorites-item border border-gray-200 rounded-lg overflow-hidden bg-white hover:shadow-md transition-shadow">' +
                    '<img src="' + d.image + '" alt="' + d.name + '" class="w-full h-40 object-cover bg-gray-100">' +
                    '<div class="p-3">' +
                    '<div class="product-name font-medium text-gray-900 text-sm line-clamp-2">' + d.name + '</div>' +
                    '<div class="product-price text-primary font-semibold mt-1">' + d.price + '</div>' +
                    '<div class="flex gap-2 mt-2 flex-wrap">' +
                    '<a href="' + d.url + '" class="inline-flex items-center justify-center px-3 py-1.5 rounded-lg bg-primary text-white text-sm font-medium hover:opacity-90 transition">Ürüne Git</a>' +
                    '<button type="button" class="remove-fav-btn inline-flex items-center justify-center px-3 py-1.5 rounded-lg border border-red-300 text-red-600 text-sm font-medium hover:bg-red-50 transition" data-id="' + escHtml(String(item.id)) + '">Kaldır</button>' +
                    '</div></div></div>';
            }).join('') +
            '</div>';

        const self = this;
        if (typeof window.openModal !== 'function') {
            this.showNotification('Modal yardımcısı yüklenmedi.', 'error');
            return;
        }

        const { close } = window.openModal({
            title: 'Favorilerim (' + this.favorites.length + ')',
            titleId: 'favorites-modal-title',
            content: contentHtml,
            closeLabel: 'Kapat',
            onContentReady: function (contentEl) {
                if (!contentEl) return;
                contentEl.querySelectorAll('.remove-fav-btn').forEach(function (btn) {
                    btn.addEventListener('click', function () {
                        const id = parseInt(btn.getAttribute('data-id'), 10);
                        const product = self.favorites.find(function (p) { return p.id === id; });
                        if (product) self.toggleFavorite(id, product);
                        close();
                    });
                });
            }
        });
    }

    // Karşılaştırma sayfasına yönlendir
    async showCompare() {
        if (this.compareList.length === 0) {
            this.showNotification('Henüz karşılaştırmak için ürün eklememişsiniz.', 'info');
            return;
        }

        const slugs = this.compareList.map(p => p.slug).filter(s => s).join(':');
        if (!slugs) {
            this.showNotification('Ürün bilgileri eksik. Lütfen tekrar deneyin.', 'error');
            return;
        }

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
