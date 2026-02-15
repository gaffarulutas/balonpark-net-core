/**
 * Dark Mode Toggle – Public site only (admin hariç)
 * Best practices: localStorage persistence, prefers-color-scheme, no FOUC
 * FOUC: head'de inline script ile ilk render öncesi theme uygulanır
 */
(function () {
  'use strict';

  var STORAGE_KEY = 'balonpark-theme';
  var ATTR_THEME = 'data-theme';

  /**
   * Resolve theme: 'dark' | 'light' | 'auto'
   * localStorage > system preference
   */
  function getResolvedTheme(stored) {
    if (stored === 'dark' || stored === 'light') return stored;
    if (typeof window.matchMedia !== 'function') return 'light';
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  function applyTheme(isDark) {
    var html = document.documentElement;
    if (isDark) {
      html.classList.add('dark');
      html.setAttribute(ATTR_THEME, 'dark');
    } else {
      html.classList.remove('dark');
      html.setAttribute(ATTR_THEME, 'light');
    }
    updateThemeColor(isDark);
  }

  function updateThemeColor(isDark) {
    var meta = document.querySelector('meta[name="theme-color"]');
    if (meta) {
      meta.setAttribute('content', isDark ? '#0f172a' : '#fbfbfb');
    }
  }

  function setStoredTheme(value) {
    try {
      if (value) {
        localStorage.setItem(STORAGE_KEY, value);
      } else {
        localStorage.removeItem(STORAGE_KEY);
      }
    } catch (e) {
      /* ignore */
    }
  }

  function getStoredTheme() {
    try {
      return localStorage.getItem(STORAGE_KEY);
    } catch (e) {
      return null;
    }
  }

  function toggleTheme() {
    var stored = getStoredTheme();
    var resolved = getResolvedTheme(stored);
    var next = resolved === 'dark' ? 'light' : 'dark';
    setStoredTheme(next);
    applyTheme(next === 'dark');
    updateToggleUI(next === 'dark');
  }

  function updateToggleUI(isDark) {
    var btn = document.getElementById('dark-mode-toggle');
    if (!btn) return;
    var sunIcon = btn.querySelector('.dark-mode-icon-sun');
    var moonIcon = btn.querySelector('.dark-mode-icon-moon');
    if (sunIcon) sunIcon.classList.toggle('hidden', !isDark);
    if (moonIcon) moonIcon.classList.toggle('hidden', isDark);
    btn.setAttribute('aria-label', isDark ? 'Aydınlık moda geç' : 'Karanlık moda geç');
    btn.setAttribute('title', isDark ? 'Aydınlık mod' : 'Karanlık mod');
  }

  function init() {
    var stored = getStoredTheme();
    var isDark = getResolvedTheme(stored) === 'dark';
    applyTheme(isDark);
    updateToggleUI(isDark);

    var btn = document.getElementById('dark-mode-toggle');
    if (btn) {
      btn.addEventListener('click', function (e) {
        e.preventDefault();
        toggleTheme();
      });
    }

    if (typeof window.matchMedia === 'function') {
      window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function () {
        var s = getStoredTheme();
        if (s !== 'dark' && s !== 'light') {
          var dark = getResolvedTheme(s);
          applyTheme(dark === 'dark');
          updateToggleUI(dark === 'dark');
        }
      });
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }

  window.BalonParkDarkMode = {
    toggle: toggleTheme,
    isDark: function () { return document.documentElement.classList.contains('dark'); },
    getStored: getStoredTheme,
    setStored: setStoredTheme
  };
})();
