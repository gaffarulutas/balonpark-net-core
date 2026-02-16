/**
 * Product detail page: Swiper gallery, PDF download, product code copy, admin inline edit, lightGallery.
 */
(function () {
    'use strict';

    var productMainSwiper;
    var productThumbsSwiper;

    function initSwipers() {
        if (typeof window.Swiper === 'undefined') {
            console.warn('Swiper not loaded; product gallery will not be interactive.');
            return;
        }
        var mainSwiperElement = document.querySelector('.productMainSwiper');
        var thumbsSwiperElement = document.querySelector('.productThumbsSwiper');
        if (!mainSwiperElement) return;

        var thumbsNavNext = document.querySelector('.product-gallery-thumbs-next');
        var thumbsNavPrev = document.querySelector('.product-gallery-thumbs-prev');
        if (thumbsSwiperElement) {
            productThumbsSwiper = new window.Swiper('.productThumbsSwiper', {
                spaceBetween: 6,
                slidesPerView: 'auto',
                freeMode: false,
                watchSlidesProgress: true,
                breakpoints: {
                    320: { slidesPerView: 3, spaceBetween: 6 },
                    640: { slidesPerView: 4, spaceBetween: 6 },
                    768: { slidesPerView: 5, spaceBetween: 6 },
                    1024: { slidesPerView: 6, spaceBetween: 6 },
                },
                navigation: thumbsNavNext && thumbsNavPrev ? {
                    nextEl: thumbsNavNext,
                    prevEl: thumbsNavPrev,
                } : undefined,
                allowTouchMove: true,
            });
        }

        var slideCount = mainSwiperElement.querySelectorAll('.swiper-slide').length;
        var swiperConfig = {
            spaceBetween: 0,
            speed: 300,
            autoHeight: false,
            navigation: {
                nextEl: mainSwiperElement.querySelector('.product-gallery-next'),
                prevEl: mainSwiperElement.querySelector('.product-gallery-prev'),
            },
            pagination: {
                el: mainSwiperElement.querySelector('.product-gallery-pagination'),
                type: 'fraction',
                formatFractionCurrent: function (n) { return n; },
                formatFractionTotal: function (n) { return n; },
            },
            loop: slideCount > 1,
            on: {
                slideChange: function () {
                    if (productThumbsSwiper && !productThumbsSwiper.destroyed) {
                        var realIndex = this.realIndex;
                        productThumbsSwiper.slideTo(realIndex, 300);
                    }
                },
            },
        };
        if (productThumbsSwiper) {
            swiperConfig.thumbs = {
                swiper: productThumbsSwiper,
                autoScrollOffset: 2,
            };
        }

        productMainSwiper = new window.Swiper('.productMainSwiper', swiperConfig);

        if (productThumbsSwiper && productMainSwiper) {
            productThumbsSwiper.on('click', function () {
                var clickedIndex = productThumbsSwiper.clickedIndex;
                if (typeof clickedIndex === 'number' && clickedIndex >= 0) {
                    if (productMainSwiper.params.loop) {
                        productMainSwiper.slideToLoop(clickedIndex, 300);
                    } else {
                        productMainSwiper.slideTo(clickedIndex, 300);
                    }
                }
            });
        }
    }

    function initPdfButton() {
        var pdfBtn = document.getElementById('productPdfBtn');
        if (!pdfBtn) return;
        pdfBtn.addEventListener('click', function (e) {
            e.preventDefault();
            var href = this.getAttribute('href');
            if (!href) return;
            var icon = this.querySelector('.pdf-btn-icon');
            var text = this.querySelector('.pdf-btn-text');
            this.setAttribute('disabled', 'disabled');
            this.classList.add('opacity-70', 'pointer-events-none');
            if (text) text.textContent = 'Hazırlanıyor...';
            if (icon) icon.style.display = 'none';
            var spinner = document.createElement('span');
            spinner.className = 'inline-block w-[18px] h-[18px] rounded-full flex-shrink-0 animate-spin';
            spinner.style.borderWidth = '2px';
            spinner.style.borderStyle = 'solid';
            spinner.style.borderColor = '#e5e7eb transparent #e5e7eb #e5e7eb';
            spinner.style.borderTopColor = '#dc2626';
            spinner.setAttribute('aria-hidden', 'true');
            this.insertBefore(spinner, this.firstChild);
            fetch(href)
                .then(function (res) { return res.ok ? res.blob() : Promise.reject(new Error('PDF oluşturulamadı')); })
                .then(function (blob) {
                    var url = window.URL.createObjectURL(blob);
                    var a = document.createElement('a');
                    a.href = url;
                    var pathSegments = (typeof window !== 'undefined' && window.location.pathname) ? window.location.pathname.split('/').filter(Boolean) : [];
                    var slug = pathSegments.length >= 3 ? pathSegments[pathSegments.length - 1] : 'urun';
                    a.download = 'Urun-' + slug + '.pdf';
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    window.URL.revokeObjectURL(url);
                })
                .catch(function () {
                    if (text) text.textContent = 'Hata';
                })
                .finally(function () {
                    if (text && text.textContent === 'Hazırlanıyor...') text.textContent = 'PDF indir';
                    if (text && text.textContent === 'Hata') setTimeout(function () { if (text) text.textContent = 'PDF indir'; }, 2000);
                    if (icon) icon.style.display = '';
                    var s = pdfBtn.querySelector('.animate-spin');
                    if (s) s.remove();
                    pdfBtn.removeAttribute('disabled');
                    pdfBtn.classList.remove('opacity-70', 'pointer-events-none');
                });
        });
    }

    function initProductCodeCopy() {
        document.querySelectorAll('.product-code-copy').forEach(function (btn) {
            btn.addEventListener('click', function () {
                var code = this.getAttribute('data-code');
                if (!code) return;
                navigator.clipboard.writeText(code).then(function () {
                    var origTitle = btn.getAttribute('title');
                    btn.setAttribute('title', 'Kopyalandı!');
                    btn.setAttribute('aria-label', 'Kopyalandı!');
                    setTimeout(function () {
                        btn.setAttribute('title', origTitle || 'Kodu kopyala');
                        btn.setAttribute('aria-label', 'Ürün kodunu kopyala');
                    }, 1500);
                });
            });
        });
    }

    var EDIT_MODE_STORAGE_KEY = 'productDetailEditMode';

    function initEditModeSwitch() {
        var root = document.getElementById('product-detail-root');
        var switchEl = document.getElementById('product-detail-edit-mode-switch');
        if (!root || !switchEl) return;

        var saved = sessionStorage.getItem(EDIT_MODE_STORAGE_KEY);
        if (saved === 'true') {
            switchEl.checked = true;
            root.setAttribute('data-edit-mode', 'true');
        }

        switchEl.addEventListener('change', function () {
            var on = switchEl.checked;
            root.setAttribute('data-edit-mode', on ? 'true' : 'false');
            sessionStorage.setItem(EDIT_MODE_STORAGE_KEY, on ? 'true' : 'false');
        });
    }

    function initAdminInlineEdit() {
        var container = document.querySelector('.admin-editable-field');
        if (!container) return;

        document.querySelectorAll('.admin-editable-field').forEach(function (wrap) {
            var editBtn = wrap.querySelector('.admin-edit-icon');
            if (!editBtn) return;

            editBtn.addEventListener('click', function (e) {
                var root = document.getElementById('product-detail-root');
                if (root && root.getAttribute('data-edit-mode') !== 'true') return;
                e.preventDefault();
                e.stopPropagation();
                var productId = parseInt(wrap.getAttribute('data-product-id'), 10);
                var field = wrap.getAttribute('data-field');
                var dataType = wrap.getAttribute('data-type') || 'text';
                var currentVal = wrap.getAttribute('data-value') || '';
                var valueEl = wrap.querySelector('.admin-editable-value');
                if (valueEl) {
                    if (dataType === 'textarea') currentVal = valueEl.innerHTML || valueEl.innerText || '';
                    else if (dataType !== 'bool' && !currentVal) currentVal = valueEl.innerText || valueEl.textContent || '';
                }

                var existingEditor = document.querySelector('.admin-edit-inline');
                if (existingEditor) existingEditor.remove();

                var isNumber = dataType === 'number';
                var isTextarea = dataType === 'textarea';
                var isBool = dataType === 'bool';
                var input;
                if (isBool) {
                    input = document.createElement('label');
                    input.className = 'admin-edit-input flex items-center gap-2 cursor-pointer';
                    var cb = document.createElement('input');
                    cb.type = 'checkbox';
                    cb.checked = currentVal === 'true' || currentVal === '1';
                    cb.className = 'rounded border-gray-300 text-primary focus:ring-primary';
                    var lbl = document.createElement('span');
                    lbl.textContent = 'Aktif';
                    lbl.className = 'text-sm font-medium text-gray-700';
                    input.appendChild(cb);
                    input.appendChild(lbl);
                } else {
                    input = isTextarea ? document.createElement('textarea') : document.createElement('input');
                    if (isTextarea) {
                        input.rows = 6;
                        input.className = 'admin-edit-input w-full text-sm border border-gray-300 rounded-lg p-3 focus:ring-2 focus:ring-primary focus:border-primary';
                        input.value = (valueEl && valueEl.innerText) ? valueEl.innerText : currentVal;
                    } else {
                        input.type = isNumber ? 'number' : 'text';
                        input.min = isNumber ? '0' : undefined;
                        input.className = 'admin-edit-input text-sm border border-gray-300 rounded-lg px-2 py-1.5 focus:ring-2 focus:ring-primary focus:border-primary';
                        input.value = currentVal;
                    }
                }

                var editorDiv = document.createElement('div');
                editorDiv.className = 'admin-edit-inline mt-2 flex flex-col gap-2';
                var btnRow = document.createElement('div');
                btnRow.className = 'flex gap-2';
                var saveBtn = document.createElement('button');
                saveBtn.type = 'button';
                saveBtn.className = 'inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-primary text-white text-sm font-medium hover:opacity-90 transition';
                saveBtn.innerHTML = '<i data-lucide="check" class="w-4 h-4"></i> Kaydet';
                var cancelBtn = document.createElement('button');
                cancelBtn.type = 'button';
                cancelBtn.className = 'inline-flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-gray-300 bg-white text-gray-700 text-sm font-medium hover:bg-gray-50 transition';
                cancelBtn.innerHTML = '<i data-lucide="x" class="w-4 h-4"></i> İptal';

                wrap.classList.add('admin-edit-active');
                if (valueEl) valueEl.classList.add('hidden');
                editBtn.classList.add('hidden');
                editorDiv.appendChild(input);
                btnRow.appendChild(saveBtn);
                btnRow.appendChild(cancelBtn);
                editorDiv.appendChild(btnRow);
                wrap.appendChild(editorDiv);
                if (isBool && input.querySelector) {
                    var c = input.querySelector('input');
                    if (c) c.focus();
                } else if (input.focus) input.focus();

                if (typeof lucide !== 'undefined' && lucide.createIcons) lucide.createIcons();

                function closeEditor() {
                    editorDiv.remove();
                    wrap.classList.remove('admin-edit-active');
                    if (valueEl) valueEl.classList.remove('hidden');
                    editBtn.classList.remove('hidden');
                }

                cancelBtn.addEventListener('click', function () { closeEditor(); });

                function setLoading(loading) {
                    editorDiv.classList.toggle('admin-edit-loading', loading);
                    saveBtn.disabled = loading;
                    cancelBtn.disabled = loading;
                    if (isBool && input.querySelector) {
                        var chk = input.querySelector('input[type="checkbox"]');
                        if (chk) chk.disabled = loading;
                    } else if (input.disabled !== undefined) input.disabled = loading;
                    if (loading) {
                        saveBtn.innerHTML = '<span class="admin-edit-spinner w-4 h-4 border-2 border-white border-t-transparent rounded-full inline-block"></span> Kaydediliyor...';
                    } else {
                        saveBtn.innerHTML = '<i data-lucide="check" class="w-4 h-4"></i> Kaydet';
                        if (typeof lucide !== 'undefined' && lucide.createIcons) lucide.createIcons();
                    }
                }

                saveBtn.addEventListener('click', function () {
                    var val;
                    if (isBool) {
                        var cb = input.querySelector ? input.querySelector('input[type="checkbox"]') : input;
                        val = cb ? cb.checked : false;
                    } else {
                        val = input.value.trim();
                        if (isNumber) val = val === '' ? 0 : parseFloat(val);
                    }
                    setLoading(true);
                    fetch('/api/ProductField/update', {
                        method: 'POST',
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify({ productId: productId, field: field, value: isBool ? val : (isNumber ? val : val) }),
                    })
                        .then(function (r) { return r.json().then(function (data) { return { ok: r.ok, data: data }; }); })
                        .then(function (result) {
                            if (result.ok && result.data.success) {
                                if (result.data.redirectPath) {
                                    window.location.href = result.data.redirectPath;
                                    return;
                                }
                                if (field === 'Name') {
                                    if (valueEl) valueEl.textContent = val;
                                    wrap.setAttribute('data-value', val);
                                    closeEditor();
                                } else {
                                    window.location.reload();
                                    return;
                                }
                            } else {
                                alert(result.data.message || 'Güncelleme yapılamadı.');
                                closeEditor();
                            }
                        })
                        .catch(function () {
                            alert('Bağlantı hatası.');
                            closeEditor();
                        })
                        .finally(function () {
                            setLoading(false);
                        });
                });
            });
        });
    }

    function initLightGallery() {
        var lightGalleryElement = document.getElementById('lightgallery');
        if (!lightGalleryElement || typeof lightGallery === 'undefined') return;

        var ariaDescribedby = lightGalleryElement.getAttribute('data-aria-describedby') || 'Product Gallery';
        lightGallery(lightGalleryElement, {
            speed: 500,
            licenseKey: '0000-0000-000-0000',
            plugins: [lgThumbnail, lgZoom, lgAutoplay, lgFullscreen, lgRotate, lgShare, lgHash, lgPager],
            thumbnail: true,
            animateThumb: true,
            showThumbByDefault: true,
            thumbWidth: 100,
            thumbHeight: '80px',
            thumbMargin: 5,
            zoom: true,
            scale: 1,
            actualSize: true,
            actualSizeIcons: { zoomIn: 'lg-zoom-in', zoomOut: 'lg-zoom-out' },
            autoplay: false,
            autoplayControls: true,
            progressBar: true,
            pause: 3000,
            fullScreen: true,
            rotate: true,
            flipHorizontal: true,
            flipVertical: true,
            rotateLeft: true,
            rotateRight: true,
            share: true,
            facebook: true,
            twitter: true,
            pinterest: true,
            hash: true,
            galleryId: 'product-gallery',
            pager: true,
            download: true,
            counter: true,
            loop: true,
            escKey: true,
            keyPress: true,
            controls: true,
            slideEndAnimation: true,
            hideControlOnEnd: false,
            mousewheel: true,
            mobileSettings: { controls: true, showCloseIcon: true, download: true, rotate: true },
            mode: 'lg-fade',
            selector: 'a',
            ariaLabelledby: 'Product Gallery',
            ariaDescribedby: ariaDescribedby,
        });
    }

    function onSwiperError(e) {
        if (e.message && e.message.includes('Swiper')) {
            var slides = document.querySelectorAll('.swiper-slide');
            slides.forEach(function (slide, index) {
                slide.style.display = index === 0 ? 'block' : 'none';
            });
        }
    }

    document.addEventListener('DOMContentLoaded', function () {
        initSwipers();
        initPdfButton();
        initProductCodeCopy();
        initEditModeSwitch();
        initAdminInlineEdit();
    });

    window.addEventListener('error', onSwiperError);

    document.addEventListener('DOMContentLoaded', function () {
        initLightGallery();
    });
})();
