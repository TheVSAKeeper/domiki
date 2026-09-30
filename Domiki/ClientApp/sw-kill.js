void self.__WB_MANIFEST;

self.addEventListener('install', () => {
    void self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil((async () => {
        const keys = await caches.keys();
        await Promise.all(keys.map((key) => caches.delete(key)));
        await self.registration.unregister();
        const windows = await self.clients.matchAll({ type: 'window' });
        for (const client of windows) {
            void client.navigate(client.url);
        }
    })());
});
