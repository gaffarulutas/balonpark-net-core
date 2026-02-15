/**
 * Google Translate Integration - Best Practices 2026
 * Public site için dil seçimi dropdown ve otomatik çeviri.
 * Kaynak dil: Türkçe (tr). Google destekli tüm diller desteklenir.
 */
(function () {
    'use strict';

    var COOKIE_NAME = 'googtrans';
    var COOKIE_PATH = '/';
    var COOKIE_MAX_AGE_DAYS = 365;
    var SOURCE_LANG = 'tr';

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

    function getStoredLang() {
        var cookies = document.cookie.split(';');
        for (var i = 0; i < cookies.length; i++) {
            var c = cookies[i].trim();
            if (c.indexOf(COOKIE_NAME + '=') === 0) {
                var val = c.substring(COOKIE_NAME.length + 1);
                if (val && val !== '/' + SOURCE_LANG + '/' + SOURCE_LANG) {
                    var m = val.match(/^\/[\w-]+\/([\w-]+)$/);
                    return m ? m[1] : null;
                }
                return null;
            }
        }
        return null;
    }

    function setTranslateCookie(targetLang) {
        if (!targetLang || targetLang === SOURCE_LANG) {
            document.cookie = COOKIE_NAME + '=; path=' + COOKIE_PATH + '; max-age=0; SameSite=Lax';
            return;
        }
        var val = '/' + SOURCE_LANG + '/' + targetLang;
        document.cookie = COOKIE_NAME + '=' + encodeURIComponent(val) + '; path=' + COOKIE_PATH +
            '; max-age=' + (COOKIE_MAX_AGE_DAYS * 86400) + '; SameSite=Lax';
    }

    function getCurrentDisplayLabel() {
        var stored = getStoredLang();
        if (!stored) return 'Türkçe';
        for (var i = 0; i < LANGUAGES.length; i++) {
            if (LANGUAGES[i].code === stored) return LANGUAGES[i].name;
        }
        return stored;
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
        btn.innerHTML = '<i data-lucide="languages" class="translate-dropdown__icon" aria-hidden="true"></i>' +
            '<span class="translate-dropdown__label">' + escapeHtml(getCurrentDisplayLabel()) + '</span>' +
            '<i data-lucide="chevron-down" class="translate-dropdown__chevron" aria-hidden="true"></i>';

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
            var isActive = (!lang.code && !getStoredLang()) || (lang.code && lang.code === getStoredLang());
            var a = document.createElement('button');
            a.type = 'button';
            a.className = 'translate-dropdown__item' + (isActive ? ' translate-dropdown__item--active' : '');
            a.textContent = lang.name;
            a.setAttribute('data-lang', lang.code);
            a.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                selectLanguage(lang.code);
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

    function selectLanguage(targetLang) {
        setTranslateCookie(targetLang || '');

        if (window.google && window.google.translate) {
            var select = document.querySelector('.goog-te-combo');
            if (select) {
                var code = targetLang || SOURCE_LANG;
                select.value = code;
                select.dispatchEvent(new Event('change'));
            } else {
                location.reload();
            }
        } else {
            location.reload();
        }
    }

    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }

    function initGoogleTranslateElement() {
        if (window.googleTranslateElementInit) return;
        window.googleTranslateElementInit = function () {
            new google.translate.TranslateElement({
                pageLanguage: SOURCE_LANG,
                includedLanguages: LANGUAGES.filter(function (l) { return l.code; }).map(function (l) { return l.code; }).join(','),
                layout: google.translate.TranslateElement.InlineLayout.SIMPLE,
                autoDisplay: false
            }, 'google-translate-element');
        };
    }

    function injectGoogleScript() {
        if (document.getElementById('google-translate-script')) return;
        var s = document.createElement('script');
        s.id = 'google-translate-script';
        s.type = 'text/javascript';
        s.src = '//translate.google.com/translate_a/element.js?cb=googleTranslateElementInit';
        s.async = true;
        document.head.appendChild(s);
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

        initGoogleTranslateElement();
        injectGoogleScript();
        mountDropdown();

        if (getStoredLang()) {
            document.documentElement.classList.add('translate-active');
        }
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
