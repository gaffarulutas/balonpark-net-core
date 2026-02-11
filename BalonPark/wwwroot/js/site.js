'use strict';

(function () {
    document.addEventListener('DOMContentLoaded', function () {
        initCategorySwiper();
        fetchCurrentCurrency();
    });

    if (window.jQuery) {
        window.jQuery(function () {
            initializeCategoryMenuHighlighting();
            initializeSemanticSearch();
            bindSearchDismissHandlers();
        });
    }

    window.toggleCategoryMenu = function toggleCategoryMenu() {
        const sidebar = document.getElementById('sidebar-col');
        const overlay = document.getElementById('sidebar-overlay');
        if (!sidebar || !overlay) {
            return;
        }
        sidebar.classList.toggle('-translate-x-full');
        sidebar.classList.toggle('translate-x-0');
        overlay.classList.toggle('hidden');
    };

    window.toggleSubCategories = function toggleSubCategories(event, categoryId) {
        event.preventDefault();
        event.stopPropagation();

        const button = event.currentTarget;
        const subcategoryList = document.getElementById(`subcategories-${categoryId}`);
        if (!subcategoryList) {
            return;
        }
        const icon = button.querySelector('i');
        const isOpen = subcategoryList.classList.contains('max-h-[1000px]');

        if (isOpen) {
            subcategoryList.classList.remove('max-h-[1000px]', 'opacity-100', 'py-2');
            subcategoryList.classList.add('max-h-0', 'opacity-0');
            button.classList.remove('bg-primary/10', 'text-primary');
            icon?.classList.remove('rotate-180');
        } else {
            subcategoryList.classList.remove('max-h-0', 'opacity-0');
            subcategoryList.classList.add('max-h-[1000px]', 'opacity-100', 'py-2');
            button.classList.add('bg-primary/10', 'text-primary');
            icon?.classList.add('rotate-180');
        }

        document.querySelectorAll('.subcategory-list').forEach(function (list) {
            if (list.id === `subcategories-${categoryId}`) {
                return;
            }
            list.classList.remove('max-h-[1000px]', 'opacity-100', 'py-2');
            list.classList.add('max-h-0', 'opacity-0');
        });

        document.querySelectorAll('.category-toggle').forEach(function (btn) {
            if (btn === button) {
                return;
            }
            btn.classList.remove('bg-primary/10', 'text-primary');
            btn.querySelector('i')?.classList.remove('rotate-180');
        });
    };

    window.setCurrency = function setCurrency(currency) {
        fetch('/api/currency/set', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ currency })
        })
            .then(function (response) { return response.json(); })
            .then(function (data) {
                if (!data.success) {
                    console.error('Currency değiştirilemedi:', data.message);
                    return;
                }
                resetCurrencyButtons();
                highlightCurrencyButton(currency);
                window.location.reload();
            })
            .catch(function (error) {
                console.error('Currency değiştirilirken hata oluştu:', error);
            });
    };

    window.showFavorites = function showFavorites() {
        if (!window.favoritesAndCompare) {
            console.error('favoritesAndCompare modülü bulunamadı.');
            return;
        }
        const favorites = window.favoritesAndCompare.getFavorites();
        if (!favorites.length) {
            alert('Favori listesi boş');
            return;
        }

        const modal = document.createElement('div');
        modal.className = 'favorites-modal-overlay';
        modal.innerHTML = `
            <div class="favorites-modal-content">
                <div class="modal-header">
                    <h3>Favori Ürünlerim (${favorites.length})</h3>
                    <button onclick="this.closest('.favorites-modal-overlay').remove()" class="close-btn">&times;</button>
                </div>
                <div class="favorites-grid">
                    ${favorites.map(function (item) {
                        return `
                            <div class="favorites-item">
                                <img src="${item.image}" alt="${item.name}" class="product-image">
                                <div class="product-info">
                                    <div class="product-name">${item.name}</div>
                                    <div class="product-price">${item.price}</div>
                                    <div class="product-actions">
                                        <a href="${item.url}" class="btn btn-primary btn-sm">Ürüne Git</a>
                                        <button class="btn btn-outline-danger btn-sm" onclick="removeFromFavorites(${item.id}); this.closest('.favorites-modal-overlay').remove();">Kaldır</button>
                                    </div>
                                </div>
                            </div>
                        `;
                    }).join('')}
                </div>
            </div>
        `;
        document.body.appendChild(modal);
    };

    window.showCompare = function showCompare() {
        if (!window.favoritesAndCompare) {
            console.error('favoritesAndCompare modülü bulunamadı.');
            return;
        }
        const compareList = window.favoritesAndCompare.getCompareList();
        if (!compareList.length) {
            alert('Karşılaştırma listesi boş');
            return;
        }
        if (compareList.length === 1) {
            alert('En az 2 ürün karşılaştırmanız gerekiyor');
            return;
        }

        const modal = document.createElement('div');
        modal.className = 'compare-modal-overlay';
        modal.innerHTML = `
            <div class="compare-modal-content">
                <div class="modal-header">
                    <h3>Ürün Karşılaştırması (${compareList.length})</h3>
                    <button onclick="this.closest('.compare-modal-overlay').remove()" class="close-btn">&times;</button>
                </div>
                <div class="compare-table-container">
                    <table class="compare-table">
                        <thead>
                            <tr>
                                <th>Özellik</th>
                                ${compareList.map(function (item) { return `<th class="product-cell">${item.name}</th>`; }).join('')}
                            </tr>
                        </thead>
                        <tbody>
                            <tr>
                                <td>Resim</td>
                                ${compareList.map(function (item) {
                                    return `
                                        <td class="product-cell">
                                            <img src="${item.image}" alt="${item.name}" class="product-image">
                                        </td>
                                    `;
                                }).join('')}
                            </tr>
                            <tr>
                                <td>Fiyat</td>
                                ${compareList.map(function (item) { return `<td class="product-price">${item.price}</td>`; }).join('')}
                            </tr>
                            <tr>
                                <td>İşlemler</td>
                                ${compareList.map(function (item) {
                                    return `
                                        <td>
                                            <a href="${item.url}" class="btn btn-primary btn-sm">Ürüne Git</a>
                                            <button class="btn btn-outline-danger btn-sm" onclick="removeFromCompare(${item.id}); this.closest('.compare-modal-overlay').remove();">Kaldır</button>
                                        </td>
                                    `;
                                }).join('')}
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>
        `;
        document.body.appendChild(modal);
    };

    window.removeFromFavorites = function removeFromFavorites(productId) {
        if (!window.favoritesAndCompare) {
            return;
        }
        const favorites = window.favoritesAndCompare.getFavorites();
        const product = favorites.find(function (item) { return item.id === productId; });
        if (product) {
            window.favoritesAndCompare.toggleFavorite(productId, product);
        }
    };

    window.removeFromCompare = function removeFromCompare(productId) {
        if (!window.favoritesAndCompare) {
            return;
        }
        const compareList = window.favoritesAndCompare.getCompareList();
        const product = compareList.find(function (item) { return item.id === productId; });
        if (product) {
            window.favoritesAndCompare.toggleCompare(productId, product);
        }
    };

    function initCategorySwiper() {
        if (typeof window.Swiper === 'undefined' || !document.querySelector('.categorySwiper')) {
            return;
        }

        // eslint-disable-next-line no-new
        new window.Swiper('.categorySwiper', {
            slidesPerView: 1,
            spaceBetween: 20,
            loop: true,
            autoplay: {
                delay: 3000,
                disableOnInteraction: false
            },
            pagination: {
                el: '.swiper-pagination',
                clickable: true
            },
            navigation: {
                nextEl: '.swiper-button-next',
                prevEl: '.swiper-button-prev'
            },
            breakpoints: {
                640: {
                    slidesPerView: 2,
                    spaceBetween: 20
                },
                768: {
                    slidesPerView: 3,
                    spaceBetween: 20
                },
                1024: {
                    slidesPerView: 4,
                    spaceBetween: 20
                },
                1200: {
                    slidesPerView: 5,
                    spaceBetween: 20
                }
            }
        });
    }

    function initializeCategoryMenuHighlighting() {
        const $ = window.jQuery;
        if (typeof $ !== 'function') {
            return;
        }

        const currentPath = window.location.pathname;
        const pathParts = currentPath.split('/').filter(function (part) { return part !== ''; });

        let categorySlug = null;
        let subCategorySlug = null;

        if (pathParts[0] === 'category' && pathParts.length === 2) {
            categorySlug = pathParts[1];
        } else if (pathParts[0] === 'category' && pathParts.length === 3) {
            categorySlug = pathParts[1];
            subCategorySlug = pathParts[2];
        } else if (pathParts[0] === 'category' && pathParts.length === 4) {
            categorySlug = pathParts[1];
            subCategorySlug = pathParts[2];
        }

        if (subCategorySlug) {
            $('.subcategory-link').each(function () {
                if ($(this).data('category-slug') === categorySlug && $(this).data('subcategory-slug') === subCategorySlug) {
                    $(this).addClass('bg-primary/10 text-primary font-medium');
                    const subcategoryList = $(this).closest('.subcategory-list')[0];
                    const categoryId = subcategoryList.id.replace('subcategories-', '');
                    subcategoryList.classList.remove('max-h-0', 'opacity-0');
                    subcategoryList.classList.add('max-h-[1000px]', 'opacity-100', 'py-2');
                    $(`.category-toggle[data-category-id="${categoryId}"]`).each(function () {
                        this.classList.add('bg-primary/10', 'text-primary');
                        this.querySelector('i')?.classList.add('rotate-180');
                    });
                    $(this).closest('.category-item').addClass('border-l-2 border-l-primary');
                }
            });
        } else if (categorySlug) {
            $('.category-link').each(function () {
                if ($(this).data('category-slug') === categorySlug) {
                    $(this).closest('.category-item').addClass('border-l-2 border-l-primary');
                }
            });
        }
    }

    function initializeSemanticSearch() {
        const $ = window.jQuery;
        if (typeof $ !== 'function' || typeof $.fn.search !== 'function') {
            return;
        }

        $('.ui.category.search').search({
            type: 'category',
            apiSettings: {
                url: '/api/search/all?q={query}',
                onResponse: function (searchResponse) {
                    const response = { results: {} };
                    if (searchResponse && Array.isArray(searchResponse.results)) {
                        searchResponse.results.forEach(function (item) {
                            if (!item?.type) {
                                return;
                            }
                            const typeKey = item.type;
                            if (!response.results[typeKey]) {
                                response.results[typeKey] = {
                                    name: getTypeDisplayName(typeKey),
                                    results: []
                                };
                            }
                            response.results[typeKey].results.push({
                                title: item.title || '',
                                url: item.url || '',
                                image: item.image || '',
                                price: item.price || '',
                                category: item.category || ''
                            });
                        });
                    }
                    return response;
                }
            },
            fields: {
                results: 'results',
                title: 'title',
                url: 'url'
            },
            minCharacters: 2,
            onSelect: function (result) {
                if (result.url) {
                    window.location.href = result.url;
                }
            }
        });
    }

    function bindSearchDismissHandlers() {
        const $ = window.jQuery;
        if (typeof $ !== 'function') {
            return;
        }

        $(document).on('click', '.ui.category.search .prompt', function (e) {
            e.stopPropagation();
            $(this).focus();
        });

        $(document).on('click', '.ui.category.search .results', function (e) {
            e.stopPropagation();
        });

        $(document).on('click', function (e) {
            if (!$(e.target).closest('.ui.category.search').length) {
                $('.ui.category.search').search('hide');
            }
        });
    }

    function getTypeDisplayName(type) {
        switch (type) {
            case 'product':
                return 'Ürünler';
            case 'category':
                return 'Kategoriler';
            case 'subcategory':
                return 'Alt Kategoriler';
            default:
                return 'Sonuçlar';
        }
    }

    function fetchCurrentCurrency() {
        fetch('/api/currency/current')
            .then(function (response) { return response.json(); })
            .then(function (data) {
                if (!data.success) {
                    return;
                }
                resetCurrencyButtons();
                highlightCurrencyButton(data.currency);
            })
            .catch(function () {
                // Intentionally swallow errors; currency fallback is harmless.
            });
    }

    function resetCurrencyButtons() {
        document.querySelectorAll('.currency-btn').forEach(function (btn) {
            btn.classList.remove('bg-ink', 'text-white', 'border-ink');
            if (btn.classList.contains('border')) {
                btn.classList.remove('text-ink', 'hover:bg-gray-100', 'border-gray-200');
                btn.classList.add('bg-white', 'border-gray-200', 'text-gray-600', 'hover:bg-gray-50');
            } else {
                btn.classList.remove('bg-white', 'border-gray-200', 'text-gray-600', 'hover:bg-gray-50');
                btn.classList.add('text-ink', 'hover:bg-gray-100');
            }
        });
    }

    function highlightCurrencyButton(currency) {
        if (!currency) {
            return;
        }
        const selectedBtn = document.querySelector(`[data-currency="${currency}"]`);
        if (!selectedBtn) {
            return;
        }
        selectedBtn.classList.remove('text-ink', 'hover:bg-gray-100', 'bg-white', 'border-gray-200', 'text-gray-600', 'hover:bg-gray-50', 'border-ink');
        selectedBtn.classList.add('bg-ink', 'text-white');
        if (selectedBtn.classList.contains('border')) {
            selectedBtn.classList.add('border-ink');
        }
    }
})();
