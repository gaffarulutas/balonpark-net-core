/**
 * Snackbar helper - tek tip bildirim için (polonel.com/snackbar)
 * Kullanım: showSnackbar('Mesaj', 'success'|'error'|'warning'|'info')
 */
(function () {
    'use strict';

    var typeStyles = {
        success: { backgroundColor: '#059669', actionTextColor: '#a7f3d0' },
        error: { backgroundColor: '#dc2626', actionTextColor: '#fecaca' },
        warning: { backgroundColor: '#d97706', actionTextColor: '#fef3c7' },
        info: { backgroundColor: '#2563eb', actionTextColor: '#bfdbfe' }
    };

    window.showSnackbar = function (message, type) {
        type = type || 'info';
        var options = typeStyles[type] || typeStyles.info;
        if (typeof Snackbar !== 'undefined') {
            Snackbar.show({
                text: message,
                pos: 'bottom-right',
                duration: 5000,
                showAction: true,
                actionText: 'Kapat',
                actionTextColor: options.actionTextColor,
                backgroundColor: options.backgroundColor
            });
        } else {
            console.warn('Snackbar yüklü değil:', message);
        }
    };
})();
