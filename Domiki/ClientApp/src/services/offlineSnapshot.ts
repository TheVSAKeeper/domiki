import { z } from 'zod';
import { gameStateSchema, type GameStateDto } from '../types/api';

const DB_NAME = 'domiki-offline';
const STORE_NAME = 'snapshot';
const RECORD_KEY = 'game-state';
const FACTS_KEY = 'wiki-facts';

const factsSchema = z.record(z.string(), z.string());

const LEGACY_BOARD_SIZE = 3;

function normalizeState(state: unknown): unknown {
    if (state == null || typeof state !== 'object') {
        return state;
    }

    return withUpgradeSeconds(withErrands(state as Record<string, unknown>));
}

// TODO: снять переходное чтение старого снимка, когда в проде не останется клиентов до «Ямской избы» –
// признак: в снимках больше не встречается поле errand.
function withErrands(raw: Record<string, unknown>): Record<string, unknown> {
    if (Array.isArray(raw.errands)) {
        return raw;
    }

    const orders = Array.isArray(raw.orders) ? raw.orders : [];
    return {
        ...raw,
        errands: raw.errand == null ? [] : [raw.errand],
        orderBoardSize: typeof raw.orderBoardSize === 'number' ? raw.orderBoardSize : Math.max(orders.length, LEGACY_BOARD_SIZE),
        orderFreeConcession: typeof raw.orderFreeConcession === 'boolean' ? raw.orderFreeConcession : false,
    };
}

// TODO: снять подстановку upgradeSeconds, когда в проде не останется снимков, снятых до оптимистичного отклика –
// признак: у уровней построек в снимках это поле есть всегда. Пока подставляется ноль: без сети деревня открывается
// только на чтение, а первый же ответ сервера приносит настоящие сроки стройки.
function withUpgradeSeconds(raw: Record<string, unknown>): Record<string, unknown> {
    const fillTypes = (types: unknown): unknown => Array.isArray(types) ? types.map(fillLevels) : types;
    return { ...raw, domikTypes: fillTypes(raw.domikTypes), purchaseAvailableDomiks: fillTypes(raw.purchaseAvailableDomiks) };
}

function fillLevels(domikType: unknown): unknown {
    if (domikType == null || typeof domikType !== 'object') {
        return domikType;
    }

    const raw = domikType as Record<string, unknown>;
    if (!Array.isArray(raw.levels)) {
        return domikType;
    }

    return {
        ...raw,
        levels: raw.levels.map((level: unknown) => level != null && typeof level === 'object' && !('upgradeSeconds' in level)
            ? { ...level, upgradeSeconds: 0 }
            : level),
    };
}

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

    const parsed = gameStateSchema.safeParse(normalizeState(record.state));
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
