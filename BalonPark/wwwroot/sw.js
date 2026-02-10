const CACHE_NAME = 'balonpark-v1.0.0';
const urlsToCache = [
  '/',
  '/assets/css/style.css',
  '/assets/css/vendors/bootstrap.css',
  '/assets/css/animate.min.css',
  '/assets/css/bulk-style.css',
  '/assets/css/vendors/animate.css',
  '/assets/semantic-ui/semantic.min.css',
  '/assets/js/jquery-3.6.0.min.js',
  '/assets/js/bootstrap/bootstrap.bundle.min.js',
  '/assets/js/script.js',
  '/assets/semantic-ui/semantic.min.js',
  '/assets/images/logo/logo.png',
  '/assets/images/no-image.png',
  '/manifest.json'
];

// Install event - cache resources
self.addEventListener('install', function(event) {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(function(cache) {
        console.log('Opened cache');
        return cache.addAll(urlsToCache);
      })
      .catch(function(error) {
        console.error('Cache install failed:', error);
      })
  );
  self.skipWaiting();
});

// Activate event - clean up old caches
self.addEventListener('activate', function(event) {
  event.waitUntil(
    caches.keys().then(function(cacheNames) {
      return Promise.all(
        cacheNames.map(function(cacheName) {
          if (cacheName !== CACHE_NAME) {
            console.log('Deleting old cache:', cacheName);
            return caches.delete(cacheName);
          }
        })
      );
    })
  );
  self.clients.claim();
});

// Fetch event - serve from cache, fallback to network
self.addEventListener('fetch', function(event) {
  // Skip non-GET requests
  if (event.request.method !== 'GET') {
    return;
  }

  // Skip requests to external domains
  if (!event.request.url.startsWith(self.location.origin)) {
    return;
  }

  event.respondWith(
    caches.match(event.request)
      .then(function(response) {
        // Cache hit - return response
        if (response) {
          return response;
        }

        // Clone the request
        const fetchRequest = event.request.clone();

        return fetch(fetchRequest).then(function(response) {
          // Check if we received a valid response
          if (!response || response.status !== 200 || response.type !== 'basic') {
            return response;
          }

          // Clone the response
          const responseToCache = response.clone();

          // Add to cache for future requests
          caches.open(CACHE_NAME)
            .then(function(cache) {
              cache.put(event.request, responseToCache);
            });

          return response;
        }).catch(function(error) {
          console.log('Fetch failed:', error);
          
          // For navigation requests, return cached index.html
          if (event.request.mode === 'navigate') {
            return caches.match('/');
          }
          
          // For other requests, return a fallback response
          return new Response('Offline - Content not available', {
            status: 503,
            statusText: 'Service Unavailable',
            headers: new Headers({
              'Content-Type': 'text/plain'
            })
          });
        });
      })
  );
});

// Background sync for form submissions
self.addEventListener('sync', function(event) {
  if (event.tag === 'background-sync') {
    event.waitUntil(doBackgroundSync());
  }
});

function doBackgroundSync() {
  // Handle background sync operations
  return Promise.resolve();
}

// Push notifications
self.addEventListener('push', function(event) {
  const options = {
    body: event.data ? event.data.text() : 'Yeni güncelleme mevcut!',
    icon: '/assets/images/logo/logo.png',
    badge: '/assets/images/logo/logo.png',
    vibrate: [100, 50, 100],
    data: {
      dateOfArrival: Date.now(),
      primaryKey: 1
    },
    actions: [
      {
        action: 'explore',
        title: 'Görüntüle',
        icon: '/assets/images/logo/logo.png'
      },
      {
        action: 'close',
        title: 'Kapat',
        icon: '/assets/images/logo/logo.png'
      }
    ]
  };

  event.waitUntil(
    self.registration.showNotification('Balon Park', options)
  );
});

// Notification click
self.addEventListener('notificationclick', function(event) {
  event.notification.close();

  if (event.action === 'explore') {
    event.waitUntil(
      clients.openWindow('/')
    );
  } else if (event.action === 'close') {
    // Just close the notification
  } else {
    // Default action - open the app
    event.waitUntil(
      clients.openWindow('/')
    );
  }
});

// Message handling
self.addEventListener('message', function(event) {
  if (event.data && event.data.type === 'SKIP_WAITING') {
    self.skipWaiting();
  }
});
