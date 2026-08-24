import { gameStateSchema, type GameStateDto } from '../types/api';

const DB_NAME = 'domiki-offline';
const STORE_NAME = 'snapshot';
const RECORD_KEY = 'game-state';

export interface OfflineSnapshot {
    playerId: number;
    savedAt: number;
    state: GameStateDto;
}

function openDatabase(): Promise<IDBDatabase | null> {
    if (!('indexedDB' in window)) {
        return Promise.resolve(null);
    }

    return new Promise(resolve => {
        const request = indexedDB.open(DB_NAME, 1);
        request.onupgradeneeded = () => {
            if (!request.result.objectStoreNames.contains(STORE_NAME)) {
                request.result.createObjectStore(STORE_NAME);
            }
        };
        request.onsuccess = () => {
            resolve(request.result);
        };
        request.onerror = () => {
            resolve(null);
        };
    });
}

async function withStore<T>(mode: IDBTransactionMode, action: (store: IDBObjectStore) => IDBRequest<T>): Promise<T | null> {
    const database = await openDatabase();
    if (database == null) {
        return null;
    }

    try {
        return await new Promise<T | null>(resolve => {
            const request = action(database.transaction(STORE_NAME, mode).objectStore(STORE_NAME));
            request.onsuccess = () => {
                resolve(request.result);
            };
            request.onerror = () => {
                resolve(null);
            };
        });
    } finally {
        database.close();
    }
}

export async function saveSnapshot(state: GameStateDto): Promise<void> {
    const snapshot: OfflineSnapshot = { playerId: state.playerId, savedAt: Date.now(), state };
    await withStore('readwrite', store => store.put(snapshot, RECORD_KEY));
}

export async function loadSnapshot(): Promise<OfflineSnapshot | null> {
    const stored = await withStore<unknown>('readonly', store => store.get(RECORD_KEY) as IDBRequest<unknown>);
    if (stored == null || typeof stored !== 'object') {
        return null;
    }

    const record = stored as Partial<OfflineSnapshot>;
    if (typeof record.playerId !== 'number' || typeof record.savedAt !== 'number') {
        return null;
    }

    const parsed = gameStateSchema.safeParse(record.state);
    if (!parsed.success || parsed.data.playerId !== record.playerId) {
        await clearSnapshot();
        return null;
    }

    return { playerId: record.playerId, savedAt: record.savedAt, state: parsed.data };
}

export async function clearSnapshot(): Promise<void> {
    await withStore('readwrite', store => store.delete(RECORD_KEY));
}
