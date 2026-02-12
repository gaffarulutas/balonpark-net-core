/**
 * Header arama autocomplete – best practices
 * - Trend aramalar (API), son aramalar (localStorage), canlı öneri (debounced API)
 * - Klavye: Ok yukarı/aşağı, Enter seç, Escape kapat
 * - ARIA: combobox/listbox, aria-expanded, aria-activedescendant
 * - Masaüstü ~10, mobil ~6 öneri sınırı
 */
(function () {
    'use strict';

    var STORAGE_KEY = 'balonpark_recent_searches';
    var RECENT_MAX = 10;
    var DEBOUNCE_MS = 300;
    var LIMIT_DESKTOP = 10;
    var LIMIT_MOBILE = 6;

    var dropdown = null;
    var trendingList = null;
    var recentList = null;
    var suggestionsList = null;
    var loadingEl = null;
    var emptyEl = null;
    var activeInput = null;
    var activeIndex = -1;
    var currentItems = []; // { text, url, type? }
    var debounceTimer = null;
    var trendingCache = null;
    var lastAbortController = null;

    /** Aynı origin veya relative URL; javascript: vb. engellemek için */
    function isSafeUrl(url) {
        if (!url || typeof url !== 'string') return false;
        var t = url.trim();
        if (t.startsWith('/')) return true;
        try {
            var origin = window.location.origin;
            return t.startsWith(origin);
        } catch (_) {
            return false;
        }
    }

    function getLimit() {
        return window.innerWidth < 640 ? LIMIT_MOBILE : LIMIT_DESKTOP;
    }

    function getRecentSearches() {
        try {
            var raw = localStorage.getItem(STORAGE_KEY);
            if (!raw) return [];
            var arr = JSON.parse(raw);
            return Array.isArray(arr) ? arr.filter(function (t) { return typeof t === 'string' && t.trim().length > 0; }) : [];
        } catch (_) {
            return [];
        }
    }

    function saveRecentSearch(text) {
        if (!text || !text.trim()) return;
        text = text.trim();
        var list = getRecentSearches().filter(function (t) { return t.toLowerCase() !== text.toLowerCase(); });
        list.unshift(text);
        list = list.slice(0, RECENT_MAX);
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(list));
        } catch (_) {}
    }

    function getInputs() {
        return Array.prototype.slice.call(document.querySelectorAll('.header-search-input'));
    }

    function showDropdown() {
        if (!dropdown) return;
        dropdown.classList.remove('hidden');
        dropdown.classList.add('header-search-dropdown-closed');
        dropdown.classList.remove('header-search-dropdown-open');
        if (activeInput) activeInput.classList.add('header-search-input-expanded');
        positionDropdown();
        dropdown.style.position = 'fixed';
        getInputs().forEach(function (inp) {
            inp.setAttribute('aria-expanded', 'true');
        });
        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                dropdown.classList.remove('header-search-dropdown-closed');
                dropdown.classList.add('header-search-dropdown-open');
            });
        });
    }

    function hideDropdown() {
        if (!dropdown) return;
        dropdown.classList.remove('header-search-dropdown-open');
        dropdown.classList.add('header-search-dropdown-closed');
        getInputs().forEach(function (inp) {
            inp.setAttribute('aria-expanded', 'false');
            inp.removeAttribute('aria-activedescendant');
        });
        activeIndex = -1;
        activeInput = null;
        var dur = 200;
        setTimeout(function () {
            dropdown.classList.add('hidden');
            dropdown.classList.remove('header-search-dropdown-closed');
            getInputs().forEach(function (inp) { inp.classList.remove('header-search-input-expanded'); });
        }, dur);
    }

    function positionDropdown() {
        if (!activeInput || !dropdown) return;
        var rect = activeInput.getBoundingClientRect();
        dropdown.style.position = 'fixed';
        dropdown.style.top = rect.bottom + 'px';
        dropdown.style.left = rect.left + 'px';
        dropdown.style.right = 'auto';
        dropdown.style.width = rect.width + 'px';
        dropdown.style.maxWidth = rect.width + 'px';
    }

    function setSectionVisibility(trending, recent, suggestions, loading, empty) {
        if (trendingList && trendingList.parentElement) {
            trendingList.parentElement.classList.toggle('hidden', !trending);
        }
        if (recentList && recentList.parentElement) {
            recentList.parentElement.classList.toggle('hidden', !recent);
        }
        if (suggestionsList && suggestionsList.parentElement) {
            suggestionsList.parentElement.classList.toggle('hidden', !suggestions);
        }
        if (loadingEl) loadingEl.classList.toggle('hidden', !loading);
        if (emptyEl) emptyEl.classList.toggle('hidden', !empty);
    }

    var optionIdCounter = 0;

    function renderTrending(terms) {
        if (!trendingList) return;
        trendingList.innerHTML = '';
        optionIdCounter = 0;
        (terms || []).forEach(function (t) {
            var a = document.createElement('a');
            a.id = 'header-search-option-' + (optionIdCounter++);
            a.href = t.url || ('/Search?q=' + encodeURIComponent(t.text));
            a.className = 'inline-flex items-center px-3 py-1.5 rounded-lg text-sm text-gray-700 bg-gray-100 hover:bg-primary/10 hover:text-primary transition-colors';
            a.textContent = t.text;
            a.setAttribute('role', 'option');
            a.setAttribute('data-search-text', t.text);
            a.setAttribute('data-search-url', a.href);
            trendingList.appendChild(a);
        });
    }

    function renderRecent(items) {
        if (!recentList) return;
        recentList.innerHTML = '';
        (items || []).forEach(function (text) {
            var li = document.createElement('li');
            li.id = 'header-search-option-' + (optionIdCounter++);
            li.setAttribute('role', 'option');
            var a = document.createElement('a');
            a.href = '/Search?q=' + encodeURIComponent(text);
            a.className = 'flex items-center gap-2 px-3 py-2 rounded-lg text-sm text-gray-700 hover:bg-gray-100 transition-colors';
            a.textContent = text;
            a.setAttribute('data-search-text', text);
            a.setAttribute('data-search-url', a.href);
            li.appendChild(a);
            recentList.appendChild(li);
        });
    }

    function renderSuggestions(results) {
        if (!suggestionsList) return;
        suggestionsList.innerHTML = '';
        currentItems = [];
        (results || []).forEach(function (r, i) {
            currentItems.push({ text: r.title, url: r.url, type: r.type });
            var li = document.createElement('li');
            li.id = 'header-search-option-' + i;
            li.setAttribute('role', 'option');
            var a = document.createElement('a');
            a.href = r.url || '#';
            a.className = 'flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-gray-700 hover:bg-gray-100 transition-colors';
            if (r.image && isSafeUrl(r.image)) {
                var img = document.createElement('img');
                img.src = r.image;
                img.alt = '';
                img.className = 'w-10 h-10 object-cover rounded flex-shrink-0';
                a.appendChild(img);
            }
            var span = document.createElement('span');
            span.textContent = r.title;
            if (r.price) {
                var price = document.createElement('span');
                price.className = 'text-primary font-medium ml-auto';
                price.textContent = r.price;
                a.appendChild(span);
                a.appendChild(price);
            } else {
                a.appendChild(span);
            }
            a.setAttribute('data-search-text', r.title);
            a.setAttribute('data-search-url', r.url || '#');
            li.appendChild(a);
            suggestionsList.appendChild(li);
        });
    }

    function fetchTrending(cb) {
        if (trendingCache) {
            if (cb) cb(trendingCache);
            return;
        }
        fetch('/api/search/trending?limit=' + getLimit())
            .then(function (res) { return res.json(); })
            .then(function (data) {
                trendingCache = (data && data.terms) ? data.terms : [];
                if (cb) cb(trendingCache);
            })
            .catch(function () { if (cb) cb([]); });
    }

    function fetchSuggestions(q, cb) {
        if (!q || q.length < 2) {
            if (cb) cb([]);
            return;
        }
        if (lastAbortController) lastAbortController.abort();
        lastAbortController = new AbortController();
        var signal = lastAbortController.signal;
        fetch('/api/search/all?q=' + encodeURIComponent(q) + '&limit=' + getLimit(), { signal: signal })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                var list = (data && data.results) ? data.results : [];
                if (cb) cb(list);
            })
            .catch(function (err) {
                if (err && err.name === 'AbortError') return;
                if (cb) cb([]);
            });
    }

    function openForInput(input) {
        activeInput = input;
        var q = (input && input.value) ? input.value.trim() : '';
        showDropdown();

        if (!q) {
            setSectionVisibility(false, false, false, true, false);
            var recent = getRecentSearches();
            fetchTrending(function (terms) {
                terms = terms || [];
                renderTrending(terms);
                renderRecent(recent);
                currentItems = terms.map(function (t) {
                    return { text: t.text, url: t.url || ('/Search?q=' + encodeURIComponent(t.text)) };
                }).concat(recent.map(function (text) {
                    return { text: text, url: '/Search?q=' + encodeURIComponent(text) };
                }));
                setSectionVisibility(terms.length > 0, recent.length > 0, false, false, false);
                activeIndex = -1;
                updateActiveDescendant();
            });
            return;
        }

        setSectionVisibility(false, false, false, true, false);
        fetchSuggestions(q, function (results) {
            var hasResults = results && results.length > 0;
            setSectionVisibility(false, false, hasResults, false, !hasResults);
            renderSuggestions(results || []);
            activeIndex = -1;
            updateActiveDescendant();
        });
    }

    function updateActiveDescendant() {
        if (!activeInput || !dropdown) return;
        var options = dropdown.querySelectorAll('[role="option"]');
        options.forEach(function (o, i) {
            o.classList.toggle('bg-primary/10', i === activeIndex);
        });
        if (activeIndex >= 0 && activeIndex < options.length) {
            var opt = options[activeIndex];
            var id = opt.id || (opt.querySelector('[id]') && opt.querySelector('[id]').id);
            if (id) activeInput.setAttribute('aria-activedescendant', id);
            opt.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        } else {
            activeInput.removeAttribute('aria-activedescendant');
        }
    }

    function selectCurrent() {
        var options = dropdown ? dropdown.querySelectorAll('[role="option"]') : [];
        if (activeIndex >= 0 && activeIndex < options.length) {
            var opt = options[activeIndex];
            var link = opt.querySelector('a') || opt;
            var url = link.getAttribute('data-search-url') || (link.href || '');
            var text = link.getAttribute('data-search-text') || (link.textContent || '').trim();
            if (text) saveRecentSearch(text);
            if (url && url !== '#' && isSafeUrl(url)) {
                window.location.href = url;
                return;
            }
        }
        if (activeInput && currentItems.length > 0 && activeIndex >= 0 && activeIndex < currentItems.length) {
            var item = currentItems[activeIndex];
            saveRecentSearch(item.text);
            if (item.url && isSafeUrl(item.url)) window.location.href = item.url;
        }
    }

    function onInputFocus(e) {
        activeInput = e.target;
        openForInput(activeInput);
    }

    function onInputInput(e) {
        activeInput = e.target;
        var q = (e.target.value || '').trim();
        clearTimeout(debounceTimer);
        if (!q) {
            openForInput(activeInput);
            return;
        }
        debounceTimer = setTimeout(function () {
            openForInput(activeInput);
        }, DEBOUNCE_MS);
    }

    function onInputKeydown(e) {
        if (!dropdown || dropdown.classList.contains('hidden')) return;
        var key = e.key;
        var options = dropdown.querySelectorAll('[role="option"]');
        if (key === 'Escape') {
            e.preventDefault();
            hideDropdown();
            return;
        }
        if (key === 'Enter') {
            e.preventDefault();
            selectCurrent();
            return;
        }
        if (key === 'ArrowDown') {
            e.preventDefault();
            activeIndex = activeIndex < options.length - 1 ? activeIndex + 1 : 0;
            updateActiveDescendant();
            return;
        }
        if (key === 'ArrowUp') {
            e.preventDefault();
            activeIndex = activeIndex <= 0 ? options.length - 1 : activeIndex - 1;
            updateActiveDescendant();
            return;
        }
    }

    function onFormSubmit(e) {
        var input = e.target && e.target.querySelector('.header-search-input');
        if (input && input.value && input.value.trim()) {
            saveRecentSearch(input.value.trim());
        }
    }

    function onDropdownClick(e) {
        var target = e.target.closest('a[data-search-url]');
        if (!target) return;
        e.preventDefault();
        var text = target.getAttribute('data-search-text');
        var url = target.getAttribute('data-search-url');
        if (text) saveRecentSearch(text);
        if (url && isSafeUrl(url)) window.location.href = url;
    }

    function onClickOutside(e) {
        if (!dropdown || dropdown.classList.contains('hidden')) return;
        var wrap = e.target.closest('#header-search-wrap-desktop, #header-search-wrap-mobile, #header-search-dropdown');
        if (!wrap) hideDropdown();
    }

    function init() {
        dropdown = document.getElementById('header-search-dropdown');
        trendingList = document.getElementById('header-search-trending-list');
        recentList = document.getElementById('header-search-recent-list');
        suggestionsList = document.getElementById('header-search-suggestions-list');
        loadingEl = document.getElementById('header-search-loading');
        emptyEl = document.getElementById('header-search-empty');
        if (!dropdown) return;

        getInputs().forEach(function (input) {
            input.addEventListener('focus', onInputFocus);
            input.addEventListener('input', onInputInput);
            input.addEventListener('keydown', onInputKeydown);
        });

        var forms = document.querySelectorAll('form[action="/Search"]');
        forms.forEach(function (form) {
            form.addEventListener('submit', onFormSubmit);
        });

        dropdown.addEventListener('click', onDropdownClick);
        document.addEventListener('click', onClickOutside);
        window.addEventListener('scroll', onScrollResize, true);
        window.addEventListener('resize', onScrollResize);
    }

    function onScrollResize() {
        if (dropdown && !dropdown.classList.contains('hidden') && activeInput) positionDropdown();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
