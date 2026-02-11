'use strict';

(function () {
    document.addEventListener('DOMContentLoaded', function () {
        initCategorySwiper();
        fetchCurrentCurrency();
        initializeLazyImages();
        initCategoryHoverPopover();
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
        const icon = button.querySelector('svg') || button.querySelector('i');
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
            var otherIcon = btn.querySelector('svg') || btn.querySelector('i');
            if (otherIcon) otherIcon.classList.remove('rotate-180');
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

    /* showFavorites, showCompare, removeFromFavorites, removeFromCompare are defined in favorites-compare.js */

    function initializeLazyImages() {
        const lazyImages = document.querySelectorAll('img.lazy-image');
        if (!lazyImages.length) {
            return;
        }

        lazyImages.forEach(function (image) {
            if (image.getAttribute('loading') === 'eager') {
                return;
            }

            image.setAttribute('loading', 'lazy');
            const container = ensureLazyContainer(image);

            const handleLoad = function () {
                container.classList.add('lazy-loaded');
                container.classList.remove('lazy-error');
            };

            const handleError = function () {
                container.classList.add('lazy-error');
            };

            if (image.complete) {
                // naturalWidth === 0 indicates a failed load
                if (image.naturalWidth === 0) {
                    handleError();
                } else {
                    handleLoad();
                }
            } else {
                image.addEventListener('load', handleLoad, { once: true });
                image.addEventListener('error', handleError, { once: true });
            }
        });
    }

    function ensureLazyContainer(image) {
        const parent = image.parentElement;
        if (!parent) {
            return document.body;
        }

        parent.classList.add('lazy-image-container');

        if (!parent.querySelector('.lazy-spinner')) {
            const spinner = document.createElement('span');
            spinner.className = 'lazy-spinner';
            spinner.setAttribute('aria-hidden', 'true');
            parent.appendChild(spinner);
        }

        return parent;
    }

    /**
     * Kategori mega menü (2026 UX): hover ile sağda tek panel açılır.
     * Panel soldaki liste ile aynı yükseklikte, bitişik görünüm; içerik grid.
     * Açılış/kapanış gecikmesi ile yanıp sönme önlenir; panel hover'da açık kalır.
     */
    function initCategoryHoverPopover() {
        var nav = document.querySelector('.js-category-popover-nav');
        if (!nav) {
            return;
        }
        var isDesktop = window.matchMedia && window.matchMedia('(min-width: 768px)').matches;
        if (!isDesktop) {
            return;
        }
        var triggers = nav.querySelectorAll('.category-popover-trigger');
        var panel = nav.querySelector('.mega-menu-panel') || nav.querySelector('.category-popover-panel');
        var contents = panel ? panel.querySelectorAll('.mega-panel-content') : [];
        if (!triggers.length || !panel) {
            return;
        }
        var openDelay = 60;
        var closeDelay = 120;
        var openTimer = null;
        var closeTimer = null;
        var activeTrigger = null;

        function openPanel(trigger) {
            if (closeTimer) {
                clearTimeout(closeTimer);
                closeTimer = null;
            }
            var categoryId = trigger.getAttribute('data-category-id');
            if (activeTrigger) {
                activeTrigger.setAttribute('aria-expanded', 'false');
                activeTrigger.classList.remove('is-dropdown-open');
            }
            activeTrigger = trigger;
            trigger.setAttribute('aria-expanded', 'true');
            trigger.classList.add('is-dropdown-open');
            contents.forEach(function (el) {
                if (el.getAttribute('data-category-id') === categoryId) {
                    el.classList.remove('hidden');
                    el.setAttribute('aria-hidden', 'false');
                } else {
                    el.classList.add('hidden');
                    el.setAttribute('aria-hidden', 'true');
                }
            });
            panel.style.opacity = '1';
            panel.style.visibility = 'visible';
            panel.style.pointerEvents = 'auto';
            nav.classList.add('mega-menu-open');
        }

        function closePanel() {
            if (openTimer) {
                clearTimeout(openTimer);
                openTimer = null;
            }
            if (activeTrigger) {
                activeTrigger.setAttribute('aria-expanded', 'false');
                activeTrigger.classList.remove('is-dropdown-open');
                activeTrigger = null;
            }
            contents.forEach(function (el) {
                el.classList.add('hidden');
                el.setAttribute('aria-hidden', 'true');
            });
            panel.style.opacity = '0';
            panel.style.visibility = 'hidden';
            panel.style.pointerEvents = 'none';
            nav.classList.remove('mega-menu-open');
        }

        triggers.forEach(function (trigger) {
            trigger.addEventListener('mouseenter', function () {
                if (closeTimer) {
                    clearTimeout(closeTimer);
                    closeTimer = null;
                }
                if (openTimer) {
                    clearTimeout(openTimer);
                    openTimer = null;
                }
                openTimer = setTimeout(function () {
                    openTimer = null;
                    openPanel(trigger);
                }, openDelay);
            });

            trigger.addEventListener('mouseleave', function () {
                closeTimer = setTimeout(function () {
                    closeTimer = null;
                    closePanel();
                }, closeDelay);
            });
        });

        if (panel) {
            panel.addEventListener('mouseenter', function () {
                if (closeTimer) {
                    clearTimeout(closeTimer);
                    closeTimer = null;
                }
            });
            panel.addEventListener('mouseleave', function () {
                closeTimer = setTimeout(function () {
                    closeTimer = null;
                    closePanel();
                }, closeDelay);
            });
        }
    }

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
