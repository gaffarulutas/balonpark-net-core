/**
 * SweetAlert2 (Swal): confirm yerine onay diyaloğu.
 * API: window.swalConfirm({ title, text?, html?, icon? }) → Promise<{ isConfirmed }>
 * icon: 'question' (genel onay), 'warning' (silme/geri alınamaz), 'info' (bilgi)
 */
(function () {
    'use strict';

    var DEFAULTS = {
        icon: 'question',
        showCancelButton: true,
        confirmButtonText: 'Evet',
        cancelButtonText: 'İptal',
        focusCancel: true,
        customClass: {
            confirmButton: 'swal2-confirm',
            cancelButton: 'swal2-cancel'
        }
    };

    function swalConfirm(options) {
        if (typeof window.Swal === 'undefined') {
            return Promise.resolve({ isConfirmed: window.confirm((options && (options.title || options.text || options.html)) || 'Onaylıyor musunuz?') });
        }
        var opts = typeof options === 'string' ? { title: options } : (options || {});
        return window.Swal.fire(Object.assign({}, DEFAULTS, opts));
    }

    window.swalConfirm = swalConfirm;
})();
