/**
 * Google Translate Integration - Best Practices 2026
 * Deprecated widget yerine URL redirect yöntemi kullanılır (%100 güvenilir).
 * Kaynak dil: Türkçe (tr).
 */
(function () {
    'use strict';

    var COOKIE_NAME = 'balonpark_translate_lang';
    var COOKIE_PATH = '/';
    var COOKIE_MAX_AGE_DAYS = 365;
    var SOURCE_LANG = 'tr';
    var GOOGLE_TRANSLATE_URL = 'https://translate.google.com/translate';

    /** Localhost'ta Google Translate çevirisi çalışmaz; feature flag ile devre dışı bırakılır */
    function isLocalhost() {
        try {
            var h = (window.location.hostname || '').toLowerCase();
            return h === 'localhost' || h === '127.0.0.1' || h === '[::1]';
        } catch (e) {
            return false;
        }
    }

    /**
     * Google Translate destekli diller (ISO-639 + yaygın varyantlar)
     * Cloud Translation API / Google Translate ile uyumlu
     */
    var LANGUAGES = [
        { code: '', name: 'Türkçe (Orijinal)' },
        { code: 'en', name: 'English' },
        { code: 'de', name: 'Deutsch' },
        { code: 'fr', name: 'Français' },
        { code: 'es', name: 'Español' },
        { code: 'it', name: 'Italiano' },
        { code: 'pt', name: 'Português' },
        { code: 'pt-BR', name: 'Português (Brasil)' },
        { code: 'ru', name: 'Русский' },
        { code: 'zh-CN', name: '中文 (简体)' },
        { code: 'zh-TW', name: '中文 (繁體)' },
        { code: 'ja', name: '日本語' },
        { code: 'ko', name: '한국어' },
        { code: 'ar', name: 'العربية' },
        { code: 'hi', name: 'हिन्दी' },
        { code: 'pl', name: 'Polski' },
        { code: 'nl', name: 'Nederlands' },
        { code: 'tr', name: 'Türkçe' },
        { code: 'vi', name: 'Tiếng Việt' },
        { code: 'th', name: 'ไทย' },
        { code: 'id', name: 'Bahasa Indonesia' },
        { code: 'ms', name: 'Bahasa Melayu' },
        { code: 'sv', name: 'Svenska' },
        { code: 'da', name: 'Dansk' },
        { code: 'no', name: 'Norsk' },
        { code: 'fi', name: 'Suomi' },
        { code: 'el', name: 'Ελληνικά' },
        { code: 'he', name: 'עברית' },
        { code: 'hu', name: 'Magyar' },
        { code: 'cs', name: 'Čeština' },
        { code: 'ro', name: 'Română' },
        { code: 'bg', name: 'Български' },
        { code: 'uk', name: 'Українська' },
        { code: 'hr', name: 'Hrvatski' },
        { code: 'sk', name: 'Slovenčina' },
        { code: 'sl', name: 'Slovenščina' },
        { code: 'sr', name: 'Српски' },
        { code: 'bn', name: 'বাংলা' },
        { code: 'fa', name: 'فارسی' },
        { code: 'ur', name: 'اردو' },
        { code: 'sw', name: 'Kiswahili' },
        { code: 'af', name: 'Afrikaans' },
        { code: 'sq', name: 'Shqip' },
        { code: 'am', name: 'አማርኛ' },
        { code: 'hy', name: 'Հայերեն' },
        { code: 'az', name: 'Azərbaycan' },
        { code: 'eu', name: 'Euskara' },
        { code: 'be', name: 'Беларуская' },
        { code: 'bs', name: 'Bosanski' },
        { code: 'ca', name: 'Català' },
        { code: 'et', name: 'Eesti' },
        { code: 'tl', name: 'Filipino' },
        { code: 'fy', name: 'Frysk' },
        { code: 'gl', name: 'Galego' },
        { code: 'ka', name: 'ქართული' },
        { code: 'gu', name: 'ગુજરાતી' },
        { code: 'ha', name: 'Hausa' },
        { code: 'iw', name: 'עברית' },
        { code: 'is', name: 'Íslenska' },
        { code: 'ga', name: 'Gaeilge' },
        { code: 'kn', name: 'ಕನ್ನಡ' },
        { code: 'km', name: 'ខ្មែរ' },
        { code: 'ky', name: 'Кыргызча' },
        { code: 'lo', name: 'ລາວ' },
        { code: 'lv', name: 'Latviešu' },
        { code: 'lt', name: 'Lietuvių' },
        { code: 'lb', name: 'Lëtzebuergesch' },
        { code: 'mk', name: 'Македонски' },
        { code: 'ml', name: 'മലയാളം' },
        { code: 'mt', name: 'Malti' },
        { code: 'mr', name: 'मराठी' },
        { code: 'mn', name: 'Монгол' },
        { code: 'ne', name: 'नेपाली' },
        { code: 'nb', name: 'Norsk Bokmål' },
        { code: 'pa', name: 'ਪੰਜਾਬੀ' },
        { code: 'ta', name: 'தமிழ்' },
        { code: 'te', name: 'తెలుగు' },
        { code: 'uz', name: 'O\'zbek' },
        { code: 'cy', name: 'Cymraeg' },
        { code: 'zu', name: 'isiZulu' }
    ];

    /** Tarayıcı dil kodunu desteklenen dile eşle (en-US -> en, zh-CN -> zh-CN vb.) */
    function mapBrowserLangToSupported(browserLang) {
        if (!browserLang || typeof browserLang !== 'string') return null;
        var code = browserLang.split('-')[0].toLowerCase();
        var full = browserLang.split('-').map(function (p, i) { return i === 0 ? p.toLowerCase() : p; }).join('-');
        for (var i = 0; i < LANGUAGES.length; i++) {
            var lang = LANGUAGES[i];
            if (!lang.code) continue;
            if (lang.code.toLowerCase() === full) return lang.code;
            if (lang.code.toLowerCase() === code) return lang.code;
        }
        return null;
    }

    function getBrowserLanguage() {
        if (typeof navigator === 'undefined') return null;
        var langs = navigator.languages && navigator.languages.length ? navigator.languages : (navigator.language ? [navigator.language] : []);
        for (var i = 0; i < langs.length; i++) {
            var mapped = mapBrowserLangToSupported(langs[i]);
            if (mapped) return mapped;
        }
        return null;
    }

    function getStoredLang() {
        var cookies = document.cookie.split(';');
        for (var i = 0; i < cookies.length; i++) {
            var c = cookies[i].trim();
            if (c.indexOf(COOKIE_NAME + '=') === 0) {
                var val = c.substring(COOKIE_NAME.length + 1).trim();
                return val || null;
            }
        }
        return null;
    }

    function setTranslateCookie(targetLang) {
        if (!targetLang || targetLang === SOURCE_LANG) {
            document.cookie = COOKIE_NAME + '=; path=' + COOKIE_PATH + '; max-age=0; SameSite=Lax';
        } else {
            document.cookie = COOKIE_NAME + '=' + targetLang + '; path=' + COOKIE_PATH +
                '; max-age=' + (COOKIE_MAX_AGE_DAYS * 86400) + '; SameSite=Lax';
        }
    }

    function isOnGoogleTranslate() {
        try {
            var h = window.location.hostname;
            return h.indexOf('translate.google') !== -1 || h.indexOf('translate.googleusercontent') !== -1;
        } catch (e) {
            return false;
        }
    }

    /**
     * Google Translate proxy'deyken orijinal sayfa URL'ini bul.
     * translate.google.com / translate.googleusercontent.com URL formatlarına uyumlu.
     */
    function getOriginalPageUrl() {
        if (!isOnGoogleTranslate()) return window.location.href;
        var loc = window.location;
        var s = loc.search || '';
        var h = loc.hash || '';
        var fullQuery = s + (h.indexOf('?') === 0 ? h : h.split('?')[1] ? '?' + h.split('?')[1] : '');
        var match = (s + ' ' + fullQuery).match(/[?&]u=([^&\s#]+)/);
        if (match) {
            try {
                return decodeURIComponent(match[1].replace(/\+/g, ' '));
            } catch (e) { /* ignore */ }
        }
        try {
            if (window.parent !== window && window.parent.location.href) {
                var pm = (window.parent.location.search + ' ' + (window.parent.location.hash || '')).match(/[?&]u=([^&\s#]+)/);
                if (pm) return decodeURIComponent(pm[1].replace(/\+/g, ' '));
            }
        } catch (e) { /* cross-origin */ }
        var body = document.body;
        if (body && body.getAttribute('data-site-url')) {
            var url = body.getAttribute('data-site-url').trim();
            if (url && url.indexOf('http') === 0) return url;
        }
        return null;
    }

    function getSiteBaseUrl() {
        return window.location.origin || (window.location.protocol + '//' + window.location.host);
    }

    /** Proxy sayfasında mevcut hedef dili URL'den (tl) al; kendi sitemizde cookie'den */
    function getCurrentDisplayLang() {
        if (isOnGoogleTranslate()) {
            var loc = window.location;
            var q = (loc.search || '') + (loc.hash || '').split('?')[1] ? '?' + (loc.hash || '').split('?')[1] : '';
            var m = q.match(/[?&]tl=([^&\s#]+)/);
            if (m) {
                try {
                    var tl = decodeURIComponent(m[1]).trim().toLowerCase();
                    if (tl === SOURCE_LANG || tl === 'tr') return '';
                    return tl;
                } catch (e) { /* ignore */ }
            }
            try {
                if (window.parent !== window && window.parent.location.search) {
                    var pm = window.parent.location.search.match(/[?&]tl=([^&\s#]+)/);
                    if (pm) return decodeURIComponent(pm[1]).trim();
                }
            } catch (e) { /* cross-origin */ }
        }
        return getStoredLang();
    }

    function getCurrentDisplayLabel() {
        var code = getCurrentDisplayLang();
        if (!code) return 'Türkçe';
        for (var i = 0; i < LANGUAGES.length; i++) {
            if (LANGUAGES[i].code === code) return LANGUAGES[i].name;
        }
        return code;
    }

    function createDropdown() {
        var wrapper = document.createElement('div');
        wrapper.className = 'translate-dropdown';
        wrapper.setAttribute('role', 'combobox');
        wrapper.setAttribute('aria-label', 'Dil seçin');
        wrapper.setAttribute('aria-expanded', 'false');
        wrapper.setAttribute('aria-haspopup', 'listbox');

        var btn = document.createElement('button');
        btn.type = 'button';
        btn.className = 'translate-dropdown__trigger';
        btn.setAttribute('aria-label', 'Dil seçin');
        btn.innerHTML = '<span class="translate-dropdown__trigger-content">' +
            '<i data-lucide="languages" class="translate-dropdown__icon" aria-hidden="true"></i>' +
            '<span class="translate-dropdown__label">' + escapeHtml(getCurrentDisplayLabel()) + '</span>' +
            '<i data-lucide="chevron-down" class="translate-dropdown__chevron" aria-hidden="true"></i>' +
            '</span><span class="translate-dropdown__loading" aria-hidden="true">' +
            '<span class="translate-dropdown__spinner"></span>Çeviriliyor...</span>';

        var panel = document.createElement('div');
        panel.className = 'translate-dropdown__panel';
        panel.setAttribute('role', 'listbox');
        panel.setAttribute('aria-label', 'Desteklenen diller');
        panel.setAttribute('hidden', '');

        var list = document.createElement('ul');
        list.className = 'translate-dropdown__list';
        LANGUAGES.forEach(function (lang) {
            var li = document.createElement('li');
            li.setAttribute('role', 'option');
            var current = getCurrentDisplayLang();
            var isActive = (!lang.code && !current) || (lang.code && lang.code === current);
            var a = document.createElement('button');
            a.type = 'button';
            a.className = 'translate-dropdown__item' + (isActive ? ' translate-dropdown__item--active' : '');
            a.textContent = lang.name;
            a.setAttribute('data-lang', lang.code);
            a.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                selectLanguage(lang.code, wrapper);
                closeDropdown();
            });
            li.appendChild(a);
            list.appendChild(li);
        });
        panel.appendChild(list);
        wrapper.appendChild(btn);
        wrapper.appendChild(panel);

        btn.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            toggleDropdown();
        });

        document.addEventListener('click', function (e) {
            if (!wrapper.contains(e.target)) closeDropdown();
        });

        panel.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeDropdown();
        });

        function toggleDropdown() {
            var open = wrapper.classList.toggle('translate-dropdown--open');
            if (open) {
                panel.removeAttribute('hidden');
            } else {
                panel.setAttribute('hidden', '');
            }
            wrapper.setAttribute('aria-expanded', open ? 'true' : 'false');
            if (open && typeof window.lucide !== 'undefined' && window.lucide.createIcons) {
                window.lucide.createIcons();
            }
        }

        function closeDropdown() {
            wrapper.classList.remove('translate-dropdown--open');
            panel.setAttribute('hidden', '');
            wrapper.setAttribute('aria-expanded', 'false');
        }

        return wrapper;
    }

    function selectLanguage(targetLang, wrapper) {
        var currentLang = getStoredLang();
        var newLang = targetLang || '';
        if ((!newLang && !currentLang) || (newLang && newLang === currentLang)) {
            return;
        }
        var dropdown = wrapper || document.querySelector('.translate-dropdown');
        if (dropdown) {
            dropdown.classList.add('translate-dropdown--loading');
            var trigger = dropdown.querySelector('.translate-dropdown__trigger');
            if (trigger) trigger.setAttribute('disabled', '');
            var panel = dropdown.querySelector('.translate-dropdown__panel');
            if (panel) panel.setAttribute('hidden', '');
        }
        setTranslateCookie(newLang);
        if (isLocalhost()) {
            /* Localhost: Google Translate erişilemez; sadece cookie güncelle, yönlendirme yapma */
            if (dropdown) {
                dropdown.classList.remove('translate-dropdown--loading');
                var t = dropdown.querySelector('.translate-dropdown__trigger');
                if (t) t.removeAttribute('disabled');
                var lbl = dropdown.querySelector('.translate-dropdown__label');
                if (lbl) lbl.textContent = getCurrentDisplayLabel();
            }
            return;
        }
        var targetUrl;
        if (!newLang || newLang === SOURCE_LANG) {
            targetUrl = isOnGoogleTranslate() ? getOriginalPageUrl() : window.location.href;
            if (!targetUrl) targetUrl = getSiteBaseUrl() + '/';
            targetUrl = targetUrl.replace(/#.*$/, '');
            setTimeout(function () { window.location.href = targetUrl; }, 50);
        } else {
            var pageUrl = isOnGoogleTranslate() ? getOriginalPageUrl() : window.location.href;
            if (!pageUrl) pageUrl = getSiteBaseUrl() + window.location.pathname + window.location.search;
            targetUrl = GOOGLE_TRANSLATE_URL + '?sl=' + SOURCE_LANG + '&tl=' + encodeURIComponent(newLang) + '&u=' + encodeURIComponent(pageUrl);
            setTimeout(function () { window.location.href = targetUrl; }, 50);
        }
    }

    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }

    function mountDropdown() {
        var container = document.querySelector('.site-header__actions');
        if (!container) return;
        var dropdown = createDropdown();
        var currencyFirst = container.querySelector('.currency-btn');
        if (currencyFirst) {
            container.insertBefore(dropdown, currencyFirst);
        } else {
            container.appendChild(dropdown);
        }
        if (typeof window.lucide !== 'undefined' && window.lucide.createIcons) {
            window.lucide.createIcons();
        }
    }

    function init() {
        var translateContainer = document.getElementById('google-translate-element');
        if (!translateContainer) return;

        /* Google Translate proxy'deyse (çevrilmiş sayfa), dropdown'u göster */
        if (isOnGoogleTranslate()) {
            mountDropdown();
            return;
        }
        /* Kendi sitemizde: dil tercihi yoksa tarayıcı diline göre otomatik yönlendir (localhost hariç) */
        if (!isLocalhost() && !getStoredLang()) {
            var browserLang = getBrowserLanguage();
            if (browserLang && browserLang !== SOURCE_LANG) {
                setTranslateCookie(browserLang);
                var pageUrl = window.location.href;
                window.location.href = GOOGLE_TRANSLATE_URL + '?sl=' + SOURCE_LANG + '&tl=' + encodeURIComponent(browserLang) + '&u=' + encodeURIComponent(pageUrl);
                return;
            }
        }
        mountDropdown();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.BalonParkTranslate = {
        selectLanguage: selectLanguage,
        getStoredLang: getStoredLang,
        getCurrentDisplayLabel: getCurrentDisplayLabel
    };
})();
