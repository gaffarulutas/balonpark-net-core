/**
 * Generic Tailwind modal helper – best practices:
 * - Fixed overlay, high z-index, centered content
 * - Focus trap inside modal, focus restore on close
 * - Escape key closes, backdrop click closes
 * - aria-modal, aria-labelledby, role="dialog"
 * - body scroll lock while open
 *
 * Usage:
 *   const { close, contentElement } = openModal({
 *     title: 'Başlık',
 *     titleId: 'my-modal-title',
 *     content: '<div>...</div>',
 *     onContentReady: (el) => { /* bind buttons *\/ },
 *     onClose: () => {}
 *   });
 */
(function () {
    'use strict';

    var FOCUSABLE = 'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])';

    function getFocusables(container) {
        if (!container || !container.querySelectorAll) return [];
        return Array.prototype.filter.call(
            container.querySelectorAll(FOCUSABLE),
            function (el) { return !el.hasAttribute('disabled') && el.offsetParent !== null; }
        );
    }

    function trapFocus(e, container) {
        var focusables = getFocusables(container);
        if (focusables.length === 0) return;
        var first = focusables[0];
        var last = focusables[focusables.length - 1];
        if (e.key !== 'Tab') return;
        if (e.shiftKey) {
            if (document.activeElement === first) {
                e.preventDefault();
                last.focus();
            }
        } else {
            if (document.activeElement === last) {
                e.preventDefault();
                first.focus();
            }
        }
    }

    window.openModal = function (options) {
        options = options || {};
        var title = options.title || '';
        var titleId = options.titleId || 'generic-modal-title';
        var content = options.content != null ? options.content : '';
        var closeLabel = options.closeLabel || 'Kapat';
        var onClose = typeof options.onClose === 'function' ? options.onClose : function () {};
        var onContentReady = typeof options.onContentReady === 'function' ? options.onContentReady : function () {};
        var contentClassName = options.contentClassName || 'bg-white rounded-xl shadow-2xl w-full max-w-2xl max-h-[90vh] overflow-hidden flex flex-col focus:outline-none modal-panel';

        var previousActive = document.activeElement;
        var overlay = document.createElement('div');
        overlay.setAttribute('role', 'dialog');
        overlay.setAttribute('aria-modal', 'true');
        overlay.setAttribute('aria-labelledby', titleId);
        overlay.setAttribute('aria-label', title || 'Modal');
        overlay.className = 'fixed inset-0 flex items-center justify-center p-4 bg-black/50 backdrop-blur-sm modal-overlay modal-overlay-enter';
        overlay.style.zIndex = '99999';
        overlay.tabIndex = -1;

        overlay.innerHTML =
            '<div class="' + contentClassName + ' modal-panel-enter" tabindex="-1">' +
            '  <div class="flex justify-between items-center px-4 py-4 border-b border-gray-200 bg-gray-50 flex-shrink-0">' +
            '    <h2 id="' + titleId + '" class="text-lg font-semibold text-gray-900">' + escapeHtml(title) + '</h2>' +
            '    <button type="button" class="modal-close-btn w-8 h-8 flex items-center justify-center rounded-lg text-gray-500 hover:bg-gray-200 hover:text-gray-700 transition-colors" aria-label="' + escapeHtml(closeLabel) + '">&times;</button>' +
            '  </div>' +
            '  <div class="modal-content-body flex-1 min-h-0 overflow-y-auto p-4">' + content + '</div>' +
            '</div>';

        function escapeHtml(s) {
            s = String(s == null ? '' : s);
            return s
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;')
                .replace(/"/g, '&quot;')
                .replace(/'/g, '&#39;');
        }

        function closeModal() {
            overlay.classList.remove('modal-overlay-open');
            if (panel) panel.classList.remove('modal-panel-open');
            setTimeout(function () {
                overlay.remove();
                document.body.style.overflow = '';
                if (previousActive && typeof previousActive.focus === 'function') {
                    try { previousActive.focus(); } catch (_) {}
                }
                onClose();
            }, 200);
        }

        var closeBtn = overlay.querySelector('.modal-close-btn');
        var contentBody = overlay.querySelector('.modal-content-body');
        var panel = overlay.firstElementChild;

        closeBtn.addEventListener('click', closeModal);
        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) closeModal();
        });
        overlay.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeModal();
            else trapFocus(e, panel);
        });

        document.body.style.overflow = 'hidden';
        document.body.appendChild(overlay);

        requestAnimationFrame(function () {
            requestAnimationFrame(function () {
                overlay.classList.add('modal-overlay-open');
                if (panel) panel.classList.add('modal-panel-open');
            });
        });

        if (closeBtn && typeof closeBtn.focus === 'function') {
            closeBtn.focus();
        } else {
            overlay.focus();
        }

        onContentReady(contentBody, overlay, closeModal);

        return {
            close: closeModal,
            contentElement: contentBody,
            overlay: overlay
        };
    };
})();
