import { latestChangelogId } from '../constants/changelog';

const STORAGE_KEY = 'changelog-last-seen';

const listeners = new Set<() => void>();

let lastSeenId: number | null = null;

const readStored = (): number => {
    const saved = localStorage.getItem(STORAGE_KEY);
    const parsed = saved == null ? 0 : Number(saved);
    return Number.isFinite(parsed) ? parsed : 0;
};

export const getLastSeenChangelogId = (): number => {
    lastSeenId ??= readStored();
    return lastSeenId;
};

const notify = () => {
    for (const listener of listeners) {
        listener();
    }
};

const handleStorage = (event: StorageEvent) => {
    if (event.key !== null && event.key !== STORAGE_KEY) {
        return;
    }

    const stored = readStored();
    if (stored === lastSeenId) {
        return;
    }

    lastSeenId = stored;
    notify();
};

export const subscribeChangelogSeen = (listener: () => void): (() => void) => {
    if (listeners.size === 0) {
        window.addEventListener('storage', handleStorage);
    }
    listeners.add(listener);
    return () => {
        listeners.delete(listener);
        if (listeners.size === 0) {
            window.removeEventListener('storage', handleStorage);
        }
    };
};

export const markChangelogSeen = () => {
    if (getLastSeenChangelogId() >= latestChangelogId) {
        return;
    }

    localStorage.setItem(STORAGE_KEY, String(latestChangelogId));
    lastSeenId = latestChangelogId;
    notify();
};
