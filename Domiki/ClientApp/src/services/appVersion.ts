import { useSyncExternalStore } from 'react';

let loadedId: string | null | undefined;
let updateAvailable = false;
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

    updateAvailable = true;
    reloadWhenHidden();
    listeners.forEach(listener => listener());
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
