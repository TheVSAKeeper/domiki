import { useSyncExternalStore } from 'react';

let loadedId: string | null | undefined;
let updateAvailable = false;
let waitingWorker: ServiceWorker | null = null;
let reloading = false;
const listeners = new Set<() => void>();

function loadedBuildId(): string | null {
    if (loadedId !== undefined) {
        return loadedId;
    }

    const script = document.querySelector<HTMLScriptElement>('script[type="module"][src]');
    const match = script == null ? null : /\/assets\/index-([A-Za-z0-9_-]+)\.js/.exec(script.getAttribute('src') ?? '');
    loadedId = match?.[1] ?? null;
    return loadedId;
}

function announceUpdate(worker: ServiceWorker | null): void {
    if (updateAvailable) {
        return;
    }

    updateAvailable = true;
    waitingWorker = worker;
    listeners.forEach(listener => listener());
}

function reloadWhenHidden(): void {
    if (document.hidden) {
        location.reload();
        return;
    }

    document.addEventListener('visibilitychange', () => {
        if (document.hidden) {
            location.reload();
        }
    });
}

export function reportServerVersion(serverVersion: string | null): void {
    if (updateAvailable || serverVersion == null || ('serviceWorker' in navigator && navigator.serviceWorker.controller != null)) {
        return;
    }

    const own = loadedBuildId();
    if (own == null || serverVersion === own) {
        return;
    }

    announceUpdate(null);
    reloadWhenHidden();
}

export function applyUpdate(): void {
    if (waitingWorker == null) {
        location.reload();
        return;
    }

    waitingWorker.postMessage({ type: 'SKIP_WAITING' });
}

export async function registerServiceWorker(): Promise<void> {
    if (!import.meta.env.PROD || !('serviceWorker' in navigator)) {
        return;
    }

    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (reloading) {
            return;
        }

        reloading = true;
        location.reload();
    });

    const registration = await navigator.serviceWorker.register('/sw.js');

    if (registration.waiting != null && navigator.serviceWorker.controller != null) {
        announceUpdate(registration.waiting);
    }

    registration.addEventListener('updatefound', () => {
        const installing = registration.installing;
        if (installing == null) {
            return;
        }

        installing.addEventListener('statechange', () => {
            if (installing.state === 'installed' && navigator.serviceWorker.controller != null) {
                announceUpdate(installing);
            }
        });
    });
}

function subscribe(listener: () => void): () => void {
    listeners.add(listener);
    return () => {
        listeners.delete(listener);
    };
}

export function useUpdateAvailable(): boolean {
    return useSyncExternalStore(subscribe, () => updateAvailable);
}
