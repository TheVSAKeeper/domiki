import { useSyncExternalStore } from 'react';

export type UpdateCheck = 'update' | 'fresh' | 'unknown';

let loadedId: string | null | undefined;
let updateAvailable = false;
let waitingWorker: ServiceWorker | null = null;
let registration: ServiceWorkerRegistration | null = null;
let reloading = false;
const InstallWaitMs = 8000;
const CheckTimeoutMs = 8000;
const listeners = new Set<() => void>();

export function buildIdOf(root: ParentNode): string | null {
    const script = root.querySelector<HTMLScriptElement>('script[type="module"][src]');
    const match = script == null ? null : /\/assets\/index-([A-Za-z0-9_-]+)\.js/.exec(script.getAttribute('src') ?? '');
    return match?.[1] ?? null;
}

export function loadedBuildId(): string | null {
    if (loadedId !== undefined) {
        return loadedId;
    }

    loadedId = buildIdOf(document);
    return loadedId;
}

export function needsReloadOnControllerChange(hadController: boolean, loaded: string | null, workerBuild: string | null): boolean {
    return hadController || loaded == null || loaded !== workerBuild;
}

async function workerBuildId(): Promise<string | null> {
    try {
        const response = await fetch('/index.html');
        if (!response.ok) {
            return null;
        }

        return buildIdOf(new DOMParser().parseFromString(await response.text(), 'text/html'));
    } catch {
        return null;
    }
}

function announceUpdate(worker: ServiceWorker | null): void {
    if (updateAvailable) {
        if (waitingWorker == null) {
            waitingWorker = worker;
        }

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
    if (updateAvailable || serverVersion == null) {
        return;
    }

    const own = loadedBuildId();
    if (own == null || serverVersion === own) {
        return;
    }

    if (registration != null) {
        void registration.update();
        return;
    }

    announceUpdate(null);
    reloadWhenHidden();
}

function waitForInstalling(active: ServiceWorkerRegistration): Promise<void> {
    const installing = active.installing;
    if (installing == null) {
        return Promise.resolve();
    }

    return new Promise<void>(resolve => {
        const finish = () => {
            installing.removeEventListener('statechange', onState);
            clearTimeout(timer);
            resolve();
        };

        const onState = () => {
            if (installing.state !== 'installing') {
                finish();
            }
        };

        const timer = setTimeout(finish, InstallWaitMs);
        installing.addEventListener('statechange', onState);
    });
}

function isUpdateAvailable(): boolean {
    return updateAvailable;
}

function withTimeout<T>(work: Promise<T>): Promise<T> {
    return Promise.race([
        work,
        new Promise<T>((_, reject) => setTimeout(() => reject(new Error('check-timeout')), CheckTimeoutMs)),
    ]);
}

export async function checkForUpdate(): Promise<UpdateCheck> {
    if (updateAvailable) {
        return 'update';
    }

    if (registration != null) {
        const active = registration;
        await withTimeout(active.update());
        await waitForInstalling(active);

        if (active.waiting != null && navigator.serviceWorker.controller != null) {
            announceUpdate(active.waiting);
        }

        return isUpdateAvailable() ? 'update' : 'fresh';
    }

    if (loadedBuildId() == null) {
        return 'unknown';
    }

    const response = await fetch('/healthz', { cache: 'no-store', signal: AbortSignal.timeout(CheckTimeoutMs) });
    if (!response.ok) {
        throw new Error(`check-failed-${response.status}`);
    }

    const serverVersion = response.headers.get('X-App-Version');
    if (serverVersion == null) {
        return 'unknown';
    }

    reportServerVersion(serverVersion);
    return isUpdateAvailable() ? 'update' : 'fresh';
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

    let hadController = navigator.serviceWorker.controller != null;
    navigator.serviceWorker.addEventListener('controllerchange', () => {
        if (reloading) {
            return;
        }

        reloading = true;
        const workerBuild = hadController ? Promise.resolve(null) : workerBuildId();
        void workerBuild.then(build => {
            if (needsReloadOnControllerChange(hadController, loadedBuildId(), build)) {
                location.reload();
                return;
            }

            hadController = true;
            reloading = false;
        });
    });

    let active: ServiceWorkerRegistration;
    try {
        active = await navigator.serviceWorker.register('/sw.js');
    } catch {
        return;
    }

    registration = active;

    document.addEventListener('visibilitychange', () => {
        if (!document.hidden && !updateAvailable) {
            void active.update();
        }
    });

    if (active.waiting != null && navigator.serviceWorker.controller != null) {
        announceUpdate(active.waiting);
    }

    active.addEventListener('updatefound', () => {
        const installing = active.installing;
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
    return useSyncExternalStore(subscribe, isUpdateAvailable);
}
