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
            // Favorilere ekle (ana sayfa kartıyla aynı alanlar: kategori, açıklama, boyut)
            this.favorites.push({
                id: productId,
                name: productData.name,
                price: productData.price,
                image: productData.image,
                url: productData.url,
                categoryName: productData.categoryName || '',
                description: productData.description || '',
                dimensions: productData.dimensions || '',
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
            
            const slug = (productData.slug && String(productData.slug).trim()) || this.getProductSlug(productData.url || '');
            if (!slug) {
                this.showNotification('Ürün bilgileri eksik. Lütfen tekrar deneyin.', 'error');
                return;
            }
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

    // URL veya path'ten ürün slug'ını çıkar (/category/.../productSlug)
    getProductSlug(url) {
        if (!url || !String(url).trim()) return '';
        try {
            const urlObj = new URL(url, window.location.origin);
            const parts = urlObj.pathname.split('/').filter(p => p);
            if (parts.length < 4) return ''; // en az category/sub/productSlug
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
            const icon = btn.querySelector('svg') || btn.querySelector('i');
            const text = btn.querySelector('.btn-text');

            if (isFav) {
                btn.classList.add('active');
                if (icon) {
                    if (icon.tagName === 'svg') icon.setAttribute('fill', 'currentColor');
                    else if (icon.setAttribute) icon.setAttribute('data-lucide', 'heart');
                }
                if (text) text.textContent = 'Favorilerden Çıkar';
            } else {
                btn.classList.remove('active');
                if (icon) {
                    if (icon.tagName === 'svg') icon.setAttribute('fill', 'none');
                    else if (icon.setAttribute) icon.setAttribute('data-lucide', 'heart');
                }
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
            
            const icon = btn.querySelector('svg') || btn.querySelector('i');
            const text = btn.querySelector('.btn-text');
            if (isInCompare) {
                btn.classList.add('active');
                if (text) text.textContent = 'Karşılaştırmadan Çıkar';
            } else {
                btn.classList.remove('active');
                if (text) text.textContent = 'Karşılaştırmaya Ekle';
            }
        });

        // Header'daki sayaçları güncelle
        this.updateHeaderCounters();
    }

    // Header'daki sayaçları güncelle
    updateHeaderCounters() {
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
        const favoritesCountText = document.querySelector('.favorites-count-text');
        const compareCountText = document.querySelector('.compare-count-text');
        if (favoritesCountText) {
            favoritesCountText.textContent = `${this.favorites.length} Ürün`;
        }
        if (compareCountText) {
            compareCountText.textContent = `${this.compareList.length} Ürün`;
        }
        // Header favori kalp ikonu: dolu/boş (Lucide SVG)
        document.querySelectorAll('.lucide-fav-header').forEach(function (el) {
            var svg = el.tagName === 'svg' ? el : el.querySelector('svg');
            if (svg) svg.setAttribute('fill', this.favorites.length > 0 ? 'currentColor' : 'none');
        }.bind(this));
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

    // Ürün verilerini al (ana sayfa / liste kartıyla uyumlu; kategori, açıklama, boyut da toplanır)
    getProductData(btn) {
        const dataUrl = btn.dataset.productUrl;
        const dataSlug = btn.dataset.productSlug;
        const card = btn.closest('article, .product-box, .product-card, .modern-product-card, .product-info');
        const name = card?.querySelector('.name, .product-title, h1.product-title, a[href*="/category/"]')?.textContent?.trim() || '';
        const priceElement = card?.querySelector('.price.active, .price-main.active, .price-tl, .price-usd, .price-eur, .price');
        const price = priceElement?.textContent?.trim() || '';
        const image = card?.querySelector('img')?.src || '';
        const productLink = card?.querySelector('a[href*="/category/"]');
        const url = (dataUrl && dataSlug) ? dataUrl : ((productLink?.href && productLink.href.split('/').filter(Boolean).length >= 4) ? productLink.href : window.location.href);
        const slug = dataSlug || (url ? url.split('/').filter(Boolean).pop() : '');
        const paragraphs = card ? Array.from(card.querySelectorAll('.p-3 > p')) : [];
        const categoryName = paragraphs[0]?.textContent?.trim() || '';
        const description = paragraphs.find(function (p) { return p.classList.contains('text-gray-500'); })?.textContent?.trim() || '';
        const dimensions = paragraphs.find(function (p) { return p.classList.contains('flex'); })?.textContent?.trim() || '';
        return { name, price, image, url, slug, categoryName, description, dimensions };
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

    // Favori modal içeriği – ana sayfa (Index) ürün kartıyla birebir aynı yapı
    buildFavoritesModalContent() {
        const noImageUrl = '/assets/images/no-image.png';
        const escHtml = (s) => String(s ?? '').replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;').replace(/'/g, '&#39;');
        const safeUrl = (s) => {
            const u = String(s ?? '').trim();
            if (!u || u === '#') return '#';
            if (/^\s*javascript:/i.test(u)) return '#';
            return u.replace(/"/g, '&quot;');
        };
        const escProduct = (p) => {
            const rawImage = safeUrl(p.image);
            const imageUrl = (rawImage && rawImage !== '#') ? rawImage : noImageUrl;
            return {
                name: escHtml(p.name),
                price: escHtml(p.price != null ? String(p.price) : ''),
                priceEmpty: !p.price || String(p.price).trim() === '' || /fiyat\s*için\s*iletişim/i.test(String(p.price)),
                image: imageUrl,
                url: safeUrl(p.url),
                categoryName: escHtml(p.categoryName || ''),
                description: escHtml((p.description || '').substring(0, 60)) + ((p.description || '').length > 60 ? '...' : ''),
                dimensions: escHtml(p.dimensions || '')
            };
        };

        if (this.favorites.length === 0) {
            return '<div class="empty-state py-12 text-center text-gray-500">' +
                '<p class="text-sm">Henüz favori ürün yok.</p>' +
                '<p class="text-xs mt-1">Ürün sayfalarından favorilere ekleyebilirsiniz.</p>' +
                '</div>';
        }

        return '<div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">' +
            this.favorites.map(function (item) {
                const d = escProduct(item);
                return '<article class="group favorites-item bg-white rounded border border-gray-100 overflow-hidden hover:border-gray-200 hover:bg-gray-50/50 transition-all duration-200 flex flex-col">' +
                    '<div class="relative aspect-square bg-gray-50 overflow-hidden">' +
                    '<a href="' + d.url + '" class="block w-full h-full">' +
                    '<img src="' + d.image + '" alt="' + d.name + '" class="lazy-image w-full h-full object-cover group-hover:opacity-95 transition-opacity duration-200" loading="lazy" onerror="this.onerror=null;this.src=\'' + noImageUrl + '\'">' +
                    '</a></div>' +
                    '<div class="p-3 flex flex-col flex-1 border-t border-gray-100">' +
                    '<a href="' + d.url + '" class="font-medium text-sm text-ink hover:text-primary line-clamp-2 transition">' + d.name + '</a>' +
                    (d.categoryName ? '<p class="text-xs text-gray-400 mt-0.5">' + d.categoryName + '</p>' : '') +
                    (d.description ? '<p class="text-xs text-gray-500 mt-1 line-clamp-2">' + d.description + '</p>' : '') +
                    (d.dimensions ? '<p class="text-[11px] text-gray-400 mt-1 flex items-center gap-1"><i data-lucide="ruler" class="w-3.5 h-3.5 flex-shrink-0"></i> ' + d.dimensions + '</p>' : '') +
                    '<div class="mt-auto pt-2.5">' +
                    (d.priceEmpty ? '<p class="text-xs text-gray-500">Fiyat için iletişime geçin</p>' : '<p class="price-tl text-sm font-medium text-primary">' + d.price + '</p>') +
                    '</div>' +
                    '<div class="flex gap-2 flex-wrap mt-2">' +
                    '<button type="button" class="remove-fav-btn inline-flex items-center justify-center px-3 py-1.5 rounded-lg border border-red-200 text-red-600 text-sm font-medium hover:bg-red-50 transition" data-id="' + escHtml(String(item.id)) + '">Kaldır</button>' +
                    '</div></div></article>';
            }).join('') +
            '</div>';
    }

    // Favorileri göster (modal açık kalır; Kaldır tıklanınca sadece içerik güncellenir)
    showFavorites() {
        if (this.favorites.length === 0) {
            this.showNotification('Henüz favori ürün eklememişsiniz.', 'info');
            return;
        }

        const self = this;
        if (typeof window.openModal !== 'function') {
            this.showNotification('Modal yardımcısı yüklenmedi.', 'error');
            return;
        }

        const titleId = 'favorites-modal-title';
        window.openModal({
            title: 'Favorilerim (' + this.favorites.length + ')',
            titleId: titleId,
            contentClassName: 'bg-white rounded-xl shadow-2xl w-full max-w-3xl max-h-[90vh] overflow-hidden flex flex-col focus:outline-none modal-panel',
            content: this.buildFavoritesModalContent(),
            closeLabel: 'Kapat',
            onContentReady: function (contentEl, overlayEl, closeFn) {
                if (!contentEl) return;
                if (typeof lucide !== 'undefined' && lucide.createIcons) {
                    lucide.createIcons({ root: contentEl });
                }
                self.bindFavoritesModalRemoveButtons(contentEl, overlayEl, titleId, closeFn);
            }
        });
    }

    // Modal içindeki "Kaldır" butonlarını bağla; tıklanınca sadece içeriği güncelle, modalı kapatma
    bindFavoritesModalRemoveButtons(contentEl, overlay, titleId, closeModal) {
        const self = this;
        const titleEl = overlay ? overlay.querySelector('#' + titleId) : null;

        function refreshContent() {
            if (!contentEl) return;
            contentEl.innerHTML = self.buildFavoritesModalContent();
            if (typeof lucide !== 'undefined' && lucide.createIcons) {
                lucide.createIcons({ root: contentEl });
            }
            if (titleEl) titleEl.textContent = 'Favorilerim (' + self.favorites.length + ')';
            self.updateUI();
            if (self.favorites.length === 0) {
                return;
            }
            self.bindFavoritesModalRemoveButtons(contentEl, overlay, titleId, closeModal);
        }

        contentEl.querySelectorAll('.remove-fav-btn').forEach(function (btn) {
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                const id = parseInt(btn.getAttribute('data-id'), 10);
                const product = self.favorites.find(function (p) { return p.id === id; });
                if (product) {
                    self.toggleFavorite(id, product);
                    refreshContent();
                }
            });
        });
    }

    // Karşılaştırma sayfasına yönlendir
    async showCompare() {
        if (this.compareList.length === 0) {
            this.showNotification('Henüz karşılaştırmak için ürün eklememişsiniz.', 'info');
            return;
        }
        // Slug yoksa URL'den türet (eski kayıtlar veya eksik data için)
        const slugs = this.compareList
            .map(p => (p.slug && String(p.slug).trim()) || this.getProductSlug(p.url || ''))
            .filter(s => s);
        if (slugs.length === 0) {
            this.showNotification('Ürün bilgileri eksik. Lütfen tekrar deneyin.', 'error');
            return;
        }
        window.location.href = `/Compare/${slugs.join(':')}`;
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
