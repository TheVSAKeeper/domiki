import { getPushPublicKey, subscribePush, unsubscribePush } from './api';

export type PushState = 'unsupported' | 'denied' | 'off' | 'on';

function isSupported(): boolean {
    return 'serviceWorker' in navigator && 'PushManager' in window && 'Notification' in window;
}

function urlBase64ToUint8Array(base64Url: string): Uint8Array<ArrayBuffer> {
    const padding = '='.repeat((4 - base64Url.length % 4) % 4);
    const base64 = (base64Url + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = atob(base64);
    const array = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) {
        array[i] = raw.charCodeAt(i);
    }
    return array;
}

function arrayBufferToBase64(buffer: ArrayBuffer | null): string {
    if (buffer == null) {
        return '';
    }

    return btoa(String.fromCharCode(...new Uint8Array(buffer)));
}

export async function getPushState(): Promise<PushState> {
    if (!isSupported()) {
        return 'unsupported';
    }

    if (Notification.permission === 'denied') {
        return 'denied';
    }

    const registration = await navigator.serviceWorker.getRegistration();
    const subscription = await registration?.pushManager.getSubscription();
    return subscription == null ? 'off' : 'on';
}

export async function enablePush(): Promise<void> {
    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
        throw new Error('Разрешение на уведомления не выдано');
    }

    const publicKey = await getPushPublicKey();
    if (publicKey === '') {
        throw new Error('Уведомления не настроены на сервере');
    }

    const registration = await navigator.serviceWorker.register('/sw.js');

    const subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(publicKey),
    });

    await subscribePush({
        endpoint: subscription.endpoint,
        p256dh: arrayBufferToBase64(subscription.getKey('p256dh')),
        auth: arrayBufferToBase64(subscription.getKey('auth')),
    });
}

export async function disablePush(): Promise<void> {
    const registration = await navigator.serviceWorker.getRegistration();
    const subscription = await registration?.pushManager.getSubscription();
    if (subscription == null) {
        return;
    }

    await subscription.unsubscribe();
    await unsubscribePush(subscription.endpoint);
}
