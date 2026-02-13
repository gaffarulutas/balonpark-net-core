/**
 * SweetAlert2 (Swal): onay diyaloğu.
 * API: window.swalConfirm({ title, text?, html?, icon?, confirmButtonText?, cancelButtonText? }) → Promise<{ isConfirmed }>
 * icon: 'question' (genel), 'warning' (silme/geri alınamaz), 'info' (bilgi)
 */
(function () {
    'use strict';

    var DEFAULTS = {
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Onayla',
        cancelButtonText: 'İptal',
        focusCancel: true,
        customClass: {
            confirmButton: 'swal2-confirm',
            cancelButton: 'swal2-cancel'
        }
    };

    function swalConfirm(options) {
        if (typeof window.Swal === 'undefined') {
            var msg = (options && (options.title || options.text || (options.html && options.html.replace(/<[^>]+>/g, '')))) || 'Devam etmek istiyor musunuz?';
            return Promise.resolve({ isConfirmed: window.confirm(msg) });
        }
        var opts = typeof options === 'string' ? { title: options } : (options || {});
        return window.Swal.fire(Object.assign({}, DEFAULTS, opts));
    }

    window.swalConfirm = swalConfirm;
})();
