'use strict';

(function () {
    const GA_MEASUREMENT_ID = 'G-8VRG223YDS';
    const AD_CONVERSION_TARGET = 'AW-17719505609/BlBkCPLF2L0bEMnlqIFC';
    const GTM_CONTAINER_ID = 'GTM-TV5NVNB4';

    window.dataLayer = window.dataLayer || [];

    const gtagFn = window.gtag || function gtag() {
        window.dataLayer.push(arguments);
    };

    window.gtag = gtagFn;

    gtagFn('js', new Date());
    gtagFn('config', GA_MEASUREMENT_ID);

    gtagFn('event', 'conversion', {
        send_to: AD_CONVERSION_TARGET,
        value: 1.0,
        currency: 'TRY'
    });

    (function initializeGtm(w, d, s, l, i) {
        w[l] = w[l] || [];
        w[l].push({
            'gtm.start': new Date().getTime(),
            event: 'gtm.js'
        });
        const f = d.getElementsByTagName(s)[0];
        const j = d.createElement(s);
        const dl = l !== 'dataLayer' ? `&l=${l}` : '';
        j.async = true;
        j.src = `https://www.googletagmanager.com/gtm.js?id=${i}${dl}`;
        f.parentNode?.insertBefore(j, f);
    })(window, document, 'script', 'dataLayer', GTM_CONTAINER_ID);
})();
