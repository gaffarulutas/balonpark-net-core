/**
 * Google Translate Integration — BalonPark 2026
 *
 * URL redirect yöntemiyle çeviri (deprecated widget yerine).
 * Hem eski (translate.google.com/translate?u=...) hem yeni
 * (*.translate.goog proxy) formatını destekler.
 *
 * Modül yapısı:
 *   1. CONFIG          – sabitler
 *   2. LANGUAGES       – desteklenen dil listesi
 *   3. Helpers         – genel yardımcılar (escapeHtml, getQueryParam, vb.)
 *   4. Environment     – çalışma ortamı algılama (localhost, proxy, vb.)
 *   5. Cookie          – dil tercihini okuma/yazma
 *   6. URL Resolution  – orijinal URL çıkarma
 *   7. Language Detect – mevcut/tarayıcı dil tespiti
 *   8. Navigation      – Google Translate'e yönlendirme
 *   9. UI / Dropdown   – dil seçici bileşeni
 *  10. Init            – giriş noktası
 */
(function () {
    'use strict';

    /* ======================================================================
     * 1. CONFIG
     * ====================================================================== */

    var CONFIG = {
        cookieName:      'balonpark_translate_lang',
        cookiePath:      '/',
        cookieMaxAgeDays: 365,
        sourceLang:      'tr',
        translateUrl:    'https://translate.google.com/translate',
        redirectDelay:   50,   // ms — DOM güncellemesi için küçük bekleme

        /* Proxy algılama kalıpları */
        proxyHostPatterns: ['translate.google', 'translate.googleusercontent', '.translate.goog'],
        newProxySuffix:    '.translate.goog',

        /* DOM seçicileri */
        selectors: {
            headerActions:    '.site-header__actions',
            currencyBtn:      '.currency-btn',
            translateElement: 'google-translate-element'
        },

        /* BEM sınıf adları */
        css: {
            dropdown:       'translate-dropdown',
            dropdownOpen:   'translate-dropdown--open',
            dropdownLoading:'translate-dropdown--loading',
            trigger:        'translate-dropdown__trigger',
            triggerContent: 'translate-dropdown__trigger-content',
            icon:           'translate-dropdown__icon',
            label:          'translate-dropdown__label',
            chevron:        'translate-dropdown__chevron',
            loading:        'translate-dropdown__loading',
            spinner:        'translate-dropdown__spinner',
            panel:          'translate-dropdown__panel',
            list:           'translate-dropdown__list',
            item:           'translate-dropdown__item',
            itemActive:     'translate-dropdown__item--active'
        }
    };

    /* ======================================================================
     * 2. LANGUAGES — desteklenen diller (ISO 639 + yaygın varyantlar)
     * Not: İlk giriş (code:'') "orijinal dil" seçeneğidir.
     * ====================================================================== */

    var LANGUAGES = [
        { code: '',      name: 'Türkçe (Orijinal)' },
        { code: 'en',    name: 'English' },
        { code: 'de',    name: 'Deutsch' },
        { code: 'fr',    name: 'Français' },
        { code: 'es',    name: 'Español' },
        { code: 'it',    name: 'Italiano' },
        { code: 'pt',    name: 'Português' },
        { code: 'pt-BR', name: 'Português (Brasil)' },
        { code: 'ru',    name: 'Русский' },
        { code: 'zh-CN', name: '中文 (简体)' },
        { code: 'zh-TW', name: '中文 (繁體)' },
        { code: 'ja',    name: '日本語' },
        { code: 'ko',    name: '한국어' },
        { code: 'ar',    name: 'العربية' },
        { code: 'hi',    name: 'हिन्दी' },
        { code: 'pl',    name: 'Polski' },
        { code: 'nl',    name: 'Nederlands' },
        { code: 'vi',    name: 'Tiếng Việt' },
        { code: 'th',    name: 'ไทย' },
        { code: 'id',    name: 'Bahasa Indonesia' },
        { code: 'ms',    name: 'Bahasa Melayu' },
        { code: 'sv',    name: 'Svenska' },
        { code: 'da',    name: 'Dansk' },
        { code: 'no',    name: 'Norsk' },
        { code: 'fi',    name: 'Suomi' },
        { code: 'el',    name: 'Ελληνικά' },
        { code: 'he',    name: 'עברית' },
        { code: 'hu',    name: 'Magyar' },
        { code: 'cs',    name: 'Čeština' },
        { code: 'ro',    name: 'Română' },
        { code: 'bg',    name: 'Български' },
        { code: 'uk',    name: 'Українська' },
        { code: 'hr',    name: 'Hrvatski' },
        { code: 'sk',    name: 'Slovenčina' },
        { code: 'sl',    name: 'Slovenščina' },
        { code: 'sr',    name: 'Српски' },
        { code: 'bn',    name: 'বাংলা' },
        { code: 'fa',    name: 'فارسی' },
        { code: 'ur',    name: 'اردو' },
        { code: 'sw',    name: 'Kiswahili' },
        { code: 'af',    name: 'Afrikaans' },
        { code: 'sq',    name: 'Shqip' },
        { code: 'am',    name: 'አማርኛ' },
        { code: 'hy',    name: 'Հայերեն' },
        { code: 'az',    name: 'Azərbaycan' },
        { code: 'eu',    name: 'Euskara' },
        { code: 'be',    name: 'Беларуская' },
        { code: 'bs',    name: 'Bosanski' },
        { code: 'ca',    name: 'Català' },
        { code: 'et',    name: 'Eesti' },
        { code: 'tl',    name: 'Filipino' },
        { code: 'fy',    name: 'Frysk' },
        { code: 'gl',    name: 'Galego' },
        { code: 'ka',    name: 'ქართული' },
        { code: 'gu',    name: 'ગુજરાતી' },
        { code: 'ha',    name: 'Hausa' },
        { code: 'is',    name: 'Íslenska' },
        { code: 'ga',    name: 'Gaeilge' },
        { code: 'kn',    name: 'ಕನ್ನಡ' },
        { code: 'km',    name: 'ខ្មែរ' },
        { code: 'ky',    name: 'Кыргызча' },
        { code: 'lo',    name: 'ລາວ' },
        { code: 'lv',    name: 'Latviešu' },
        { code: 'lt',    name: 'Lietuvių' },
        { code: 'lb',    name: 'Lëtzebuergesch' },
        { code: 'mk',    name: 'Македонски' },
        { code: 'ml',    name: 'മലയാളം' },
        { code: 'mt',    name: 'Malti' },
        { code: 'mr',    name: 'मराठी' },
        { code: 'mn',    name: 'Монгол' },
        { code: 'ne',    name: 'नेपाली' },
        { code: 'nb',    name: 'Norsk Bokmål' },
        { code: 'pa',    name: 'ਪੰਜਾਬੀ' },
        { code: 'ta',    name: 'தமிழ்' },
        { code: 'te',    name: 'తెలుగు' },
        { code: 'uz',    name: 'O\'zbek' },
        { code: 'cy',    name: 'Cymraeg' },
        { code: 'zu',    name: 'isiZulu' }
    ];

    /* Hızlı arama için code -> name haritası */
    var LANG_MAP = {};
    LANGUAGES.forEach(function (lang) {
        if (lang.code) LANG_MAP[lang.code.toLowerCase()] = lang;
    });

    /* ======================================================================
     * 3. HELPERS
     * ====================================================================== */

    /** XSS koruması için metin kaçışı */
    function escapeHtml(text) {
        var el = document.createElement('span');
        el.textContent = text;
        return el.innerHTML;
    }

    /**
     * Query string'den parametre değeri çıkar.
     * @param {string} queryString - "?foo=1&bar=2" formatında
     * @param {string} paramName   - aranan parametre adı
     * @returns {string|null}
     */
    function getQueryParam(queryString, paramName) {
        if (!queryString) return null;
        var regex = new RegExp('[?&]' + paramName.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '=([^&\\s#]+)');
        var match = queryString.match(regex);
        if (!match) return null;
        try {
            return decodeURIComponent(match[1].replace(/\+/g, ' '));
        } catch (e) {
            return null;
        }
    }

    /** _x_tr_* parametrelerini query string'den temizler */
    function stripTranslateParams(search) {
        if (!search) return '';
        var params = search.substring(1).split('&');
        var kept = params.filter(function (p) {
            return p.indexOf('_x_tr_') !== 0;
        });
        return kept.length ? '?' + kept.join('&') : '';
    }

    /** Google Translate redirect URL'i oluştur */
    function buildTranslateUrl(targetLang, pageUrl) {
        return CONFIG.translateUrl +
            '?sl=' + CONFIG.sourceLang +
            '&tl=' + encodeURIComponent(targetLang) +
            '&u='  + encodeURIComponent(pageUrl);
    }

    /** Lucide ikonlarını yeniden başlat (lazy-loaded olabilir) */
    function refreshLucideIcons() {
        if (typeof window.lucide !== 'undefined' && window.lucide.createIcons) {
            window.lucide.createIcons();
        }
    }

    /* ======================================================================
     * 4. ENVIRONMENT — çalışma ortamı algılama
     * ====================================================================== */

    var Env = {
        /** Localhost'ta Google Translate çalışmaz */
        isLocalhost: function () {
            try {
                var hostname = (window.location.hostname || '').toLowerCase();
                return hostname === 'localhost' ||
                       hostname === '127.0.0.1' ||
                       hostname === '[::1]';
            } catch (e) {
                return false;
            }
        },

        /** Herhangi bir Google Translate proxy'sinde miyiz? (eski + yeni) */
        isOnGoogleTranslate: function () {
            try {
                var hostname = window.location.hostname;
                return CONFIG.proxyHostPatterns.some(function (pattern) {
                    return hostname.indexOf(pattern) !== -1;
                });
            } catch (e) {
                return false;
            }
        },

        /** Yeni proxy formatı: *.translate.goog */
        isOnNewProxy: function () {
            try {
                return window.location.hostname.indexOf(CONFIG.newProxySuffix) !== -1;
            } catch (e) {
                return false;
            }
        },

        /**
         * Yeni proxy hostname'inden orijinal hostname'i çıkar.
         * balonpark-com.translate.goog -> balonpark.com
         */
        getOriginalHost: function () {
            var hostname = window.location.hostname;
            var domainPart = hostname.replace(CONFIG.newProxySuffix, '');
            return domainPart.replace(/-/g, '.');
        }
    };

    /* ======================================================================
     * 5. COOKIE — dil tercihini okuma/yazma
     * ====================================================================== */

    var Cookie = {
        /** Kayıtlı dil tercihini oku */
        get: function () {
            var prefix = CONFIG.cookieName + '=';
            var cookies = document.cookie.split(';');
            for (var i = 0; i < cookies.length; i++) {
                var entry = cookies[i].trim();
                if (entry.indexOf(prefix) === 0) {
                    var value = entry.substring(prefix.length).trim();
                    return value || null;
                }
            }
            return null;
        },

        /** Dil tercihini kaydet veya temizle */
        set: function (lang) {
            var isReset = !lang || lang === CONFIG.sourceLang;
            var maxAge  = isReset ? 0 : CONFIG.cookieMaxAgeDays * 86400;
            var value   = isReset ? '' : lang;

            document.cookie = CONFIG.cookieName + '=' + value +
                '; path=' + CONFIG.cookiePath +
                '; max-age=' + maxAge +
                '; SameSite=Lax';
        }
    };

    /* ======================================================================
     * 6. URL RESOLUTION — orijinal sayfa URL'i çıkarma
     * ====================================================================== */

    var UrlResolver = {
        /** Site ana URL'i (proxy'deyken orijinal domain) */
        getBaseUrl: function () {
            if (Env.isOnNewProxy()) {
                return window.location.protocol + '//' + Env.getOriginalHost();
            }
            return window.location.origin ||
                   (window.location.protocol + '//' + window.location.host);
        },

        /**
         * Yeni proxy URL'den orijinal URL'i geri çıkar.
         * https://balonpark-com.translate.goog/path?_x_tr_sl=tr&_x_tr_tl=en&foo=1
         * -> https://balonpark.com/path?foo=1
         */
        extractFromNewProxy: function () {
            try {
                var originalHost = Env.getOriginalHost();
                var path         = window.location.pathname;
                var cleanSearch  = stripTranslateParams(window.location.search);
                return window.location.protocol + '//' + originalHost + path + cleanSearch;
            } catch (e) {
                return null;
            }
        },

        /**
         * Eski format URL'den orijinal URL'i çıkar.
         * translate.google.com/translate?sl=tr&tl=en&u=https://balonpark.com/path
         * -> u parametresinin değeri
         */
        extractFromOldProxy: function () {
            var search = window.location.search || '';
            var hash   = window.location.hash   || '';

            /* Ana frame: ?u= parametresini kontrol et */
            var hashQuery = hash.indexOf('?') !== -1
                ? '?' + hash.split('?').slice(1).join('?')
                : '';
            var combined  = search + ' ' + hashQuery;
            var urlParam  = getQueryParam(combined, 'u');
            if (urlParam) return urlParam;

            /* iframe: parent frame'den okumayı dene */
            try {
                if (window.parent !== window) {
                    var parentSearch = window.parent.location.search || '';
                    var parentHash   = window.parent.location.hash   || '';
                    var parentParam  = getQueryParam(parentSearch + ' ' + parentHash, 'u');
                    if (parentParam) return parentParam;
                }
            } catch (e) { /* cross-origin — erişim yok */ }

            /* Fallback: data-site-url attribute */
            var body = document.body;
            if (body) {
                var siteUrl = (body.getAttribute('data-site-url') || '').trim();
                if (siteUrl && siteUrl.indexOf('http') === 0) return siteUrl;
            }

            return null;
        },

        /**
         * Mevcut sayfanın orijinal (çevrilmemiş) URL'ini döndür.
         * Proxy altında değilse mevcut URL'i olduğu gibi döndürür.
         */
        getOriginalPageUrl: function () {
            if (!Env.isOnGoogleTranslate()) return window.location.href;
            if (Env.isOnNewProxy())         return this.extractFromNewProxy();
            return this.extractFromOldProxy();
        },

        /** Mevcut sayfanın URL'i (orijinal veya olduğu gibi) */
        getCurrentPageUrl: function () {
            if (Env.isOnGoogleTranslate()) {
                return this.getOriginalPageUrl() || this.getBaseUrl() + '/';
            }
            return window.location.href;
        }
    };

    /* ======================================================================
     * 7. LANGUAGE DETECTION — dil tespiti
     * ====================================================================== */

    var LangDetect = {
        /**
         * Tarayıcı dil kodunu desteklenen dile eşle.
         * en-US -> en, zh-CN -> zh-CN
         */
        mapBrowserLang: function (browserLang) {
            if (!browserLang || typeof browserLang !== 'string') return null;

            var parts    = browserLang.split('-');
            var baseCode = parts[0].toLowerCase();
            var fullCode = parts.map(function (p, i) {
                return i === 0 ? p.toLowerCase() : p;
            }).join('-');

            /* Önce tam eşleşme (zh-CN gibi), sonra kısa kod (en gibi) */
            if (LANG_MAP[fullCode]) return LANG_MAP[fullCode].code;
            if (LANG_MAP[baseCode]) return LANG_MAP[baseCode].code;
            return null;
        },

        /** Tarayıcı tercih listesinden desteklenen ilk dili bul */
        fromBrowser: function () {
            if (typeof navigator === 'undefined') return null;
            var langs = navigator.languages && navigator.languages.length
                ? navigator.languages
                : (navigator.language ? [navigator.language] : []);

            for (var i = 0; i < langs.length; i++) {
                var mapped = this.mapBrowserLang(langs[i]);
                if (mapped) return mapped;
            }
            return null;
        },

        /**
         * Proxy sayfasındaki aktif hedef dili algıla.
         * Yeni proxy: _x_tr_tl  |  Eski proxy: tl
         * Kendi sitemizde: cookie
         */
        getCurrentLang: function () {
            if (!Env.isOnGoogleTranslate()) return Cookie.get();

            var search = window.location.search || '';

            /* Yeni proxy formatı: ?_x_tr_tl=en */
            if (Env.isOnNewProxy()) {
                var proxyLang = getQueryParam(search, '_x_tr_tl');
                if (proxyLang) {
                    var normalized = proxyLang.trim().toLowerCase();
                    return (normalized === CONFIG.sourceLang) ? '' : normalized;
                }
            }

            /* Eski format: ?tl=en (hash'te de olabilir) */
            var hash      = window.location.hash || '';
            var hashQuery = hash.indexOf('?') !== -1 ? '?' + hash.split('?').slice(1).join('?') : '';
            var combined  = search + hashQuery;
            var tlParam   = getQueryParam(combined, 'tl');
            if (tlParam) {
                var tlNorm = tlParam.trim().toLowerCase();
                return (tlNorm === CONFIG.sourceLang) ? '' : tlNorm;
            }

            /* iframe: parent frame'den oku */
            try {
                if (window.parent !== window && window.parent.location.search) {
                    var parentTl = getQueryParam(window.parent.location.search, 'tl');
                    if (parentTl) return parentTl.trim();
                }
            } catch (e) { /* cross-origin */ }

            return Cookie.get();
        },

        /** Aktif dilin kullanıcıya gösterilecek adı */
        getCurrentLabel: function () {
            var code = this.getCurrentLang();
            if (!code) return 'Türkçe';
            var entry = LANG_MAP[code.toLowerCase()];
            return entry ? entry.name : code;
        }
    };

    /* ======================================================================
     * 8. NAVIGATION — sayfa yönlendirme
     * ====================================================================== */

    var Navigation = {
        /** Orijinal (Türkçe) sayfaya dön */
        goToOriginal: function () {
            var url = UrlResolver.getCurrentPageUrl().replace(/#.*$/, '');
            this._redirect(url);
        },

        /** Google Translate üzerinden hedef dile yönlendir */
        goToTranslated: function (targetLang) {
            var pageUrl = UrlResolver.getCurrentPageUrl();
            var url     = buildTranslateUrl(targetLang, pageUrl);
            this._redirect(url);
        },

        /** Geciktirilmiş yönlendirme (DOM güncellemesine vakit tanı) */
        _redirect: function (url) {
            setTimeout(function () {
                window.location.href = url;
            }, CONFIG.redirectDelay);
        }
    };

    /* ======================================================================
     * 9. UI / DROPDOWN — dil seçici bileşeni
     * ====================================================================== */

    /**
     * Dropdown UI bileşeni.
     * Tek sorumluluk: DOM oluşturma, açma/kapama, dil seçimi tetikleme.
     */
    function Dropdown() {
        this.wrapper = null;
        this.panel   = null;
        this._build();
    }

    Dropdown.prototype = {
        /* ---- Yapı oluşturma ---- */

        _build: function () {
            var self = this;
            var css  = CONFIG.css;

            /* Wrapper */
            this.wrapper = this._el('div', css.dropdown, {
                role:           'combobox',
                'aria-label':   'Dil seçin',
                'aria-expanded': 'false',
                'aria-haspopup': 'listbox'
            });

            /* Trigger button */
            var trigger = this._createTrigger();

            /* Panel */
            this.panel = this._el('div', css.panel, {
                role:         'listbox',
                'aria-label': 'Desteklenen diller',
                'aria-hidden': 'true'
            });
            this.panel.appendChild(this._createList());

            this.wrapper.appendChild(trigger);
            this.wrapper.appendChild(this.panel);

            /* Event listeners */
            trigger.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                self.toggle();
            });

            document.addEventListener('click', function (e) {
                if (!self.wrapper.contains(e.target)) self.close();
            });

            this.panel.addEventListener('keydown', function (e) {
                if (e.key === 'Escape') self.close();
            });
        },

        _createTrigger: function () {
            var css     = CONFIG.css;
            var trigger = this._el('button', css.trigger, { 'aria-label': 'Dil seçin' });
            trigger.type = 'button';
            trigger.innerHTML =
                '<span class="' + css.triggerContent + '">' +
                    '<i data-lucide="languages" class="' + css.icon + '" aria-hidden="true"></i>' +
                    '<span class="' + css.label + '">' + escapeHtml(LangDetect.getCurrentLabel()) + '</span>' +
                    '<i data-lucide="chevron-down" class="' + css.chevron + '" aria-hidden="true"></i>' +
                '</span>' +
                '<span class="' + css.loading + '" aria-hidden="true">' +
                    '<span class="' + css.spinner + '"></span>Çeviriliyor...' +
                '</span>';
            return trigger;
        },

        _createList: function () {
            var self        = this;
            var css         = CONFIG.css;
            var currentLang = LangDetect.getCurrentLang();
            var list        = this._el('ul', css.list);

            LANGUAGES.forEach(function (lang) {
                var isActive = (!lang.code && !currentLang) ||
                               (lang.code && lang.code === currentLang);

                var li  = self._el('li', null, { role: 'option' });
                var btn = self._el('button', css.item + (isActive ? ' ' + css.itemActive : ''));
                btn.type        = 'button';
                btn.textContent = lang.name;
                btn.setAttribute('data-lang', lang.code);

                btn.addEventListener('click', function (e) {
                    e.preventDefault();
                    e.stopPropagation();
                    selectLanguage(lang.code, self.wrapper);
                    self.close();
                });

                li.appendChild(btn);
                list.appendChild(li);
            });

            return list;
        },

        /* ---- DOM yardımcısı ---- */

        _el: function (tag, className, attrs) {
            var el = document.createElement(tag);
            if (className) el.className = className;
            if (attrs) {
                Object.keys(attrs).forEach(function (key) {
                    el.setAttribute(key, attrs[key]);
                });
            }
            return el;
        },

        /* ---- Açma / Kapama ---- */

        toggle: function () {
            var isOpen = this.wrapper.classList.toggle(CONFIG.css.dropdownOpen);
            this.panel.setAttribute('aria-hidden', isOpen ? 'false' : 'true');
            this.wrapper.setAttribute('aria-expanded', String(isOpen));
            if (isOpen) refreshLucideIcons();
        },

        close: function () {
            this.wrapper.classList.remove(CONFIG.css.dropdownOpen);
            this.panel.setAttribute('aria-hidden', 'true');
            this.wrapper.setAttribute('aria-expanded', 'false');
        },

        /* ---- Loading state ---- */

        setLoading: function (loading) {
            var css     = CONFIG.css;
            var trigger = this.wrapper.querySelector('.' + css.trigger);
            if (loading) {
                this.wrapper.classList.add(css.dropdownLoading);
                if (trigger) trigger.setAttribute('disabled', '');
                this.close();
            } else {
                this.wrapper.classList.remove(css.dropdownLoading);
                if (trigger) trigger.removeAttribute('disabled');
                var label = this.wrapper.querySelector('.' + css.label);
                if (label) label.textContent = LangDetect.getCurrentLabel();
            }
        }
    };

    /* ======================================================================
     * Dil seçimi — ana iş mantığı
     * ====================================================================== */

    function selectLanguage(targetLang, wrapperEl) {
        var currentLang = Cookie.get();
        var newLang     = targetLang || '';

        /* Zaten aynı dildeyse bir şey yapma */
        var isSameLang = (!newLang && !currentLang) || (newLang === currentLang);
        if (isSameLang) return;

        /* Loading state'i göster */
        var css = CONFIG.css;
        if (wrapperEl) {
            wrapperEl.classList.add(css.dropdownLoading);
            var trigger = wrapperEl.querySelector('.' + css.trigger);
            if (trigger) trigger.setAttribute('disabled', '');
            var panel = wrapperEl.querySelector('.' + css.panel);
            if (panel) panel.setAttribute('aria-hidden', 'true');
            wrapperEl.classList.remove(css.dropdownOpen);
            wrapperEl.setAttribute('aria-expanded', 'false');
        }

        Cookie.set(newLang);

        /* Localhost'ta yönlendirme yapma, sadece UI güncelle */
        if (Env.isLocalhost()) {
            if (wrapperEl) {
                wrapperEl.classList.remove(css.dropdownLoading);
                var triggerEl = wrapperEl.querySelector('.' + css.trigger);
                if (triggerEl) triggerEl.removeAttribute('disabled');
                var label = wrapperEl.querySelector('.' + css.label);
                if (label) label.textContent = LangDetect.getCurrentLabel();
            }
            return;
        }

        /* Yönlendir */
        var isReturningToOriginal = !newLang || newLang === CONFIG.sourceLang;
        if (isReturningToOriginal) {
            Navigation.goToOriginal();
        } else {
            Navigation.goToTranslated(newLang);
        }
    }

    /* ======================================================================
     * Dropdown'ı sayfaya mount etme
     * ====================================================================== */

    function mountDropdown() {
        var container = document.querySelector(CONFIG.selectors.headerActions);
        if (!container) return;

        var dropdown    = new Dropdown();
        var currencyBtn = container.querySelector(CONFIG.selectors.currencyBtn);

        if (currencyBtn) {
            container.insertBefore(dropdown.wrapper, currencyBtn);
        } else {
            container.appendChild(dropdown.wrapper);
        }

        refreshLucideIcons();
    }

    /* ======================================================================
     * 10. INIT — giriş noktası
     * ====================================================================== */

    function init() {
        var translateElement = document.getElementById(CONFIG.selectors.translateElement);
        if (!translateElement) return;

        /* Proxy üzerindeyse veya kendi sitemizde: dropdown'u göster */
        mountDropdown();
    }

    /* DOMContentLoaded veya hemen çalıştır */
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    /* Public API */
    window.BalonParkTranslate = {
        selectLanguage:      selectLanguage,
        getStoredLang:       Cookie.get.bind(Cookie),
        getCurrentDisplayLabel: LangDetect.getCurrentLabel.bind(LangDetect)
    };

})();
