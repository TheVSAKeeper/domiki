import { z } from 'zod';
import { gameStateSchema, type GameStateDto } from '../types/api';

const DB_NAME = 'domiki-offline';
const STORE_NAME = 'snapshot';
const RECORD_KEY = 'game-state';
const FACTS_KEY = 'wiki-facts';

const factsSchema = z.record(z.string(), z.string());

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
        let request: IDBOpenDBRequest;
        try {
            request = indexedDB.open(DB_NAME, 1);
        } catch {
            resolve(null);
            return;
        }

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
            let request: IDBRequest<T>;
            try {
                request = action(database.transaction(STORE_NAME, mode).objectStore(STORE_NAME));
            } catch {
                resolve(null);
                return;
            }

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
    const stored: GameStateDto = { ...state, recap: state.recap == null ? state.recap : { awaySeconds: state.recap.awaySeconds, events: [] } };
    const snapshot: OfflineSnapshot = { playerId: stored.playerId, savedAt: Date.now(), state: stored };
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

export async function saveWikiFacts(facts: Readonly<Record<string, string>>): Promise<void> {
    await withStore('readwrite', store => store.put(facts, FACTS_KEY));
}

export async function loadWikiFacts(): Promise<Readonly<Record<string, string>>> {
    const stored = await withStore<unknown>('readonly', store => store.get(FACTS_KEY) as IDBRequest<unknown>);
    const parsed = factsSchema.safeParse(stored);
    return parsed.success ? parsed.data : {};
}

export async function clearSnapshot(): Promise<void> {
    await withStore('readwrite', store => store.delete(RECORD_KEY));
    await withStore('readwrite', store => store.delete(FACTS_KEY));
}
