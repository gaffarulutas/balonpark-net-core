/**
 * Para birimi dropdown — BalonPark (dil seçici gibi; TL, USD, EUR, RUB)
 * Header'da tek dropdown ile para birimi seçimi. Cookie ile saklanır.
 */
(function () {
    'use strict';

    var CURRENCIES = [
        { code: 'TL', name: 'Türk Lirası (₺)', symbol: '₺', icon: 'badge-turkish-lira' },
        { code: 'USD', name: 'Amerikan Doları ($)', symbol: '$', icon: 'dollar-sign' },
        { code: 'EUR', name: 'Euro (€)', symbol: '€', icon: 'euro' },
        { code: 'RUB', name: 'Rus Rublesi (₽)', symbol: '₽', icon: 'coins' }
    ];

    var CSS = {
        dropdown: 'currency-dropdown',
        dropdownOpen: 'currency-dropdown--open',
        trigger: 'currency-dropdown__trigger',
        triggerContent: 'currency-dropdown__trigger-content',
        icon: 'currency-dropdown__icon',
        label: 'currency-dropdown__label',
        chevron: 'currency-dropdown__chevron',
        panel: 'currency-dropdown__panel',
        list: 'currency-dropdown__list',
        item: 'currency-dropdown__item',
        itemActive: 'currency-dropdown__item--active'
    };

    function getCurrencyByCode(code) {
        return CURRENCIES.find(function (c) { return c.code === code; }) || CURRENCIES[0];
    }

    function el(tag, className, attrs) {
        var e = document.createElement(tag);
        if (className) e.className = className;
        if (attrs) {
            Object.keys(attrs).forEach(function (k) {
                e.setAttribute(k, attrs[k]);
            });
        }
        return e;
    }

    function buildDropdown() {
        var wrapper = el('div', CSS.dropdown, {
            role: 'combobox',
            'aria-label': 'Para birimi seçin',
            'aria-expanded': 'false',
            'aria-haspopup': 'listbox'
        });

        var current = getCurrencyByCode(window.__currencyCurrent || 'TL');
        var trigger = el('button', CSS.trigger, { type: 'button', 'aria-label': 'Para birimi seçin' });
        trigger.innerHTML =
            '<span class="' + CSS.triggerContent + '">' +
            '<i data-lucide="' + current.icon + '" class="' + CSS.icon + '" aria-hidden="true"></i>' +
            '<span class="' + CSS.label + '">' + escapeHtml(current.name) + '</span>' +
            '<i data-lucide="chevron-down" class="' + CSS.chevron + '" aria-hidden="true"></i>' +
            '</span>';

        var panel = el('div', CSS.panel, { role: 'listbox', 'aria-label': 'Para birimleri', hidden: '' });
        var list = el('ul', CSS.list);
        CURRENCIES.forEach(function (cur) {
            var isActive = (window.__currencyCurrent || 'TL') === cur.code;
            var li = el('li', null, { role: 'option' });
            var btn = el('button', CSS.item + (isActive ? ' ' + CSS.itemActive : ''));
            btn.type = 'button';
            btn.textContent = cur.name;
            btn.setAttribute('data-currency', cur.code);
            btn.addEventListener('click', function (e) {
                e.preventDefault();
                e.stopPropagation();
                if (cur.code === (window.__currencyCurrent || 'TL')) {
                    wrapper.classList.remove(CSS.dropdownOpen);
                    panel.setAttribute('hidden', '');
                    return;
                }
                if (typeof window.setCurrency === 'function') {
                    window.setCurrency(cur.code);
                } else {
                    document.cookie = 'selected_currency=' + cur.code + '; path=/; max-age=31536000';
                    window.location.reload();
                }
                wrapper.classList.remove(CSS.dropdownOpen);
                panel.setAttribute('hidden', '');
            });
            li.appendChild(btn);
            list.appendChild(li);
        });
        panel.appendChild(list);

        trigger.addEventListener('click', function (e) {
            e.preventDefault();
            e.stopPropagation();
            var open = wrapper.classList.toggle(CSS.dropdownOpen);
            wrapper.setAttribute('aria-expanded', open ? 'true' : 'false');
            if (open) panel.removeAttribute('hidden');
            else panel.setAttribute('hidden', '');
        });

        document.addEventListener('click', function (e) {
            if (!wrapper.contains(e.target)) {
                wrapper.classList.remove(CSS.dropdownOpen);
                wrapper.setAttribute('aria-expanded', 'false');
                panel.setAttribute('hidden', '');
            }
        });
        panel.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') {
                wrapper.classList.remove(CSS.dropdownOpen);
                panel.setAttribute('hidden', '');
            }
        });

        wrapper.appendChild(trigger);
        wrapper.appendChild(panel);
        return wrapper;
    }

    function escapeHtml(s) {
        var div = document.createElement('div');
        div.textContent = s;
        return div.innerHTML;
    }

    function mount() {
        var placeholder = document.getElementById('currency-dropdown-placeholder');
        if (!placeholder) return;
        fetch('/api/currency/current')
            .then(function (r) { return r.json(); })
            .then(function (data) {
                if (data.success && data.currency) {
                    window.__currencyCurrent = data.currency;
                }
            })
            .catch(function () {})
            .then(function () {
                var dropdown = buildDropdown();
                placeholder.appendChild(dropdown);
                if (typeof window.refreshLucideIcons === 'function') {
                    window.refreshLucideIcons();
                } else if (typeof lucide !== 'undefined' && lucide.createIcons) {
                    lucide.createIcons();
                }
            });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', mount);
    } else {
        mount();
    }
})();
