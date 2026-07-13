self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()));

self.addEventListener('push', (event) => {
    let title = 'Домики';
    let options = {};
    try {
        const data = event.data.json();
        title = data.title ?? title;
        options = { body: data.body, icon: '/icon-192.png', tag: 'domiki', renotify: true, data: { url: data.url } };
    } catch {
        options = { icon: '/icon-192.png', tag: 'domiki', renotify: true };
    }

    event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', (event) => {
    const url = event.notification.data?.url || '/domiki-page';
    event.notification.close();

    event.waitUntil(
        self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
            for (const client of clientList) {
                if ('focus' in client) {
                    client.focus();
                    if ('navigate' in client) {
                        client.navigate(url);
                    }
                    return;
                }
            }

            if (self.clients.openWindow) {
                return self.clients.openWindow(url);
            }
        }),
    );
});
