import { z } from 'zod';
import { reportServerVersion } from './appVersion';
import { authService } from './auth';
import { EXPIRED_COMMAND_TEXT } from '../utils/offlineOutcomeTexts';
import { clearSnapshot, loadCommands, saveCommands } from './offlineSnapshot';
import {
    decorStateSchema,
    tolokaStateSchema,
    marketStateSchema,
    commandBatchResultSchema,
    gameStateSchema,
    journalPageSchema,
    guestbookSchema,
    helpResultSchema,
    memorialPostSchema,
    problemDetailsSchema,
    relocationPlanSchema,
    villageSchema,
    worldSchema,
    villageVisitSchema,
    wikiStateSchema,
    type DecorStateDto,
    type GameCommand,
    type GameStateDto,
    type QueuedIntent,
    type OfflineOutcome,
    type JournalPageDto,
    type GuestbookDto,
    type HelpResultDto,
    type MemorialPostDto,
    type RelocationPlanDto,
    type TolokaStateDto,
    type MarketStateDto,
    type VillageDto,
    type VillageVisitDto,
    type WikiStateDto,
    type WorldDto,
} from '../types/api';

export class ApiError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'ApiError';
    }
}

export class MalformedResponseError extends ApiError {
    constructor(message: string) {
        super(message);
        this.name = 'MalformedResponseError';
    }
}

export class OfflineError extends ApiError {
    constructor(message = 'Сеть недоступна. Попробуйте позже.') {
        super(message);
        this.name = 'OfflineError';
    }
}

export class QueuedOutcomeError extends ApiError {
    constructor(message: string) {
        super(message);
        this.name = 'QueuedOutcomeError';
    }
}

let readOnly = false;

export function setReadOnlyMode(value: boolean): void {
    readOnly = value;
}

const COMMAND_ID_HEADER = 'X-Command-Id';

const WITH_STATE_HEADER = 'X-With-State';

const COMMAND_RETRY_DELAY_MS = 600;

const COMMAND_HORIZON_MS = 6 * 60 * 60 * 1000;

const COMMAND_RESEND_DELAYS_MS = [2000, 5000, 15000, 30000, 60000];

const OUTCOME_PANEL_THRESHOLD_MS = 5000;

function assertWritable(): void {
    if (readOnly) {
        throw new OfflineError('Без связи деревню можно только смотреть');
    }
}

let stateSink: ((state: GameStateDto) => void) | null = null;

export function setStateSink(sink: ((state: GameStateDto) => void) | null): void {
    stateSink = sink;
}

function newCommandId(): string {
    if (typeof crypto.randomUUID === 'function') {
        return crypto.randomUUID();
    }

    const hex = Array.from(crypto.getRandomValues(new Uint8Array(16)), byte => byte.toString(16).padStart(2, '0')).join('');
    const variant = ((parseInt(hex.slice(16, 18), 16) & 0x3f) | 0x80).toString(16).padStart(2, '0');
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-4${hex.slice(13, 16)}-${variant}${hex.slice(18, 20)}-${hex.slice(20)}`;
}

const wait = (ms: number): Promise<void> => new Promise(resolve => setTimeout(resolve, ms));

async function sinkState(res: Response): Promise<void> {
    const json: unknown = await res.json().catch(() => null);
    if (json == null) {
        return;
    }

    const parsed = gameStateSchema.safeParse(json);
    if (parsed.success) {
        stateSink?.(parsed.data);
    }
}

async function request<T>(method: 'GET' | 'POST', url: string, schema: z.ZodType<T> | null, signal?: AbortSignal, body?: unknown, commandId?: string): Promise<T> {
    let res: Response;
    try {
        const headers: Record<string, string> = {};
        if (commandId != null) {
            headers[COMMAND_ID_HEADER] = commandId;
            if (stateSink != null) {
                headers[WITH_STATE_HEADER] = '1';
            }
        }
        const init: RequestInit = {
            method,
            credentials: 'same-origin',
            signal: signal ?? null,
        };
        if (body != null) {
            headers['Content-Type'] = 'application/json';
            init.body = JSON.stringify(body);
        }
        if (Object.keys(headers).length > 0) {
            init.headers = headers;
        }
        res = await fetch(url, init);
    } catch (err) {
        if (signal?.aborted) {
            throw err;
        }
        throw new OfflineError();
    }

    reportServerVersion(res.headers.get('X-App-Version'));

    if (res.status === 401) {
        dropQueue();
        void clearSnapshot();
        authService.signIn();
        return new Promise<T>(() => {});
    }

    if (!res.ok) {
        const body: unknown = await res.json().catch(() => null);
        const problem = problemDetailsSchema.safeParse(body);
        throw new ApiError(problem.success && problem.data.detail != null ? problem.data.detail : 'Неизвестная ошибка сервера.');
    }

    if (schema == null) {
        if (commandId != null && stateSink != null) {
            await sinkState(res);
        }

        return undefined as T;
    }

    let json: unknown;
    try {
        json = await res.json();
    } catch {
        throw new MalformedResponseError('Некорректный ответ сервера.');
    }

    const parsed = schema.safeParse(json);
    if (!parsed.success) {
        throw new MalformedResponseError('Сервер вернул данные в неожиданном формате.');
    }

    return parsed.data;
}

export const apiGet = <T>(url: string, schema: z.ZodType<T>, signal?: AbortSignal): Promise<T> =>
    request<T>('GET', url, schema, signal);

export const getGameState = (signal?: AbortSignal): Promise<GameStateDto> =>
    apiGet('Domiki/GetGameState', gameStateSchema, signal);

export const getWikiState = (signal?: AbortSignal): Promise<WikiStateDto> =>
    apiGet('Domiki/GetWikiState', wikiStateSchema, signal);

export type { GameCommand, OfflineOutcome, QueuedIntent } from '../types/api';

interface QueuedCommand {
    commandId: string;
    command: GameCommand;
    queuedAtMs: number;
    playerId: number | null;
    resolve: (() => void) | null;
    reject: ((error: Error) => void) | null;
}

const MAX_COMMANDS_PER_BATCH = 50;

const queue: QueuedCommand[] = [];

let inFlight: QueuedCommand[] = [];

let commandsSink: ((intents: QueuedIntent[]) => void) | null = null;

export function setCommandsSink(sink: ((intents: QueuedIntent[]) => void) | null): void {
    commandsSink = sink;
}

let commandPlayerId: number | null = null;

export function setCommandPlayerId(playerId: number | null): void {
    commandPlayerId = playerId;
}

let failureSink: ((outcome: OfflineOutcome) => void) | null = null;

export function setCommandFailureSink(sink: ((outcome: OfflineOutcome) => void) | null): void {
    failureSink = sink;
}

function notifyCommands(): void {
    commandsSink?.([...inFlight, ...queue].map(item => ({ command: item.command, queuedAtMs: item.queuedAtMs })));
}

function queueChanged(): void {
    notifyCommands();
    void saveCommands([...inFlight, ...queue].map(item => ({
        commandId: item.commandId,
        command: item.command,
        queuedAtMs: item.queuedAtMs,
        playerId: item.playerId,
    })));
}

function settle(item: QueuedCommand, error: Error | null): void {
    if (error == null) {
        item.resolve?.();
        return;
    }

    if (Date.now() - item.queuedAtMs >= OUTCOME_PANEL_THRESHOLD_MS || item.reject == null) {
        failureSink?.({ kind: item.command.kind, reason: error.message });
        item.reject?.(new QueuedOutcomeError(error.message));
        return;
    }

    item.reject(error);
}

let resendTimer: ReturnType<typeof setTimeout> | null = null;

let resendAttempt = 0;

function scheduleResend(): void {
    if (resendTimer != null) {
        return;
    }

    const delay = COMMAND_RESEND_DELAYS_MS[Math.min(resendAttempt, COMMAND_RESEND_DELAYS_MS.length - 1)] ?? 60000;
    resendAttempt += 1;
    resendTimer = setTimeout(() => {
        resendTimer = null;
        void flushCommands();
    }, delay);
}

function clearResend(): void {
    if (resendTimer != null) {
        clearTimeout(resendTimer);
        resendTimer = null;
    }

    resendAttempt = 0;
}

function dropExpired(): void {
    const deadline = Date.now() - COMMAND_HORIZON_MS;
    const expired = queue.filter(item => item.queuedAtMs < deadline);
    if (expired.length === 0) {
        return;
    }

    const kept = queue.filter(item => item.queuedAtMs >= deadline);
    queue.length = 0;
    queue.push(...kept);
    queueChanged();
    for (const item of expired) {
        settle(item, new ApiError(EXPIRED_COMMAND_TEXT));
    }
}

let restored = false;

let queueEpoch = 0;

export function dropQueue(): void {
    queue.length = 0;
    inFlight = [];
    restored = false;
    queueEpoch += 1;
    clearResend();
    notifyCommands();
    void saveCommands([]);
}

export async function restoreCommands(): Promise<void> {
    if (restored) {
        return;
    }

    restored = true;
    const epoch = queueEpoch;
    const stored = await loadCommands();
    if (epoch !== queueEpoch) {
        return;
    }

    const known = new Set([...inFlight, ...queue].map(item => item.commandId));
    const missing = stored.filter(item => !known.has(item.commandId));
    if (missing.length === 0) {
        return;
    }

    queue.unshift(...missing.map(item => ({ ...item, resolve: null, reject: null })));
    queueChanged();
    dropExpired();
    void flushCommands();
}

let flushTimer: ReturnType<typeof setTimeout> | null = null;

let flushChain: Promise<void> = Promise.resolve();

let sending = false;

export function enqueueCommand(command: GameCommand): Promise<void> {
    let resolve!: () => void;
    let reject!: (error: Error) => void;
    const promise = new Promise<void>((resolvePromise, rejectPromise) => {
        resolve = resolvePromise;
        reject = rejectPromise;
    });

    queue.push({ commandId: newCommandId(), command, queuedAtMs: Date.now(), playerId: commandPlayerId, resolve, reject });
    queueChanged();
    if (!sending && flushTimer == null) {
        flushTimer = setTimeout(() => {
            flushTimer = null;
            void flushCommands();
        }, 0);
    }

    return promise;
}

export function flushCommands(): Promise<void> {
    if (flushTimer != null) {
        clearTimeout(flushTimer);
        flushTimer = null;
    }

    const next = flushChain.then(drainQueue, drainQueue);
    flushChain = next.catch(() => undefined);
    return next;
}

async function drainQueue(): Promise<void> {
    dropExpired();
    while (queue.length > 0) {
        const head = queue[0];
        if (head == null) {
            return;
        }

        let size = 0;
        while (size < queue.length && size < MAX_COMMANDS_PER_BATCH && queue[size]?.playerId === head.playerId) {
            size += 1;
        }

        const batch = queue.splice(0, size);
        inFlight = batch;
        sending = true;
        let delivered: boolean;
        try {
            delivered = await sendCommands(batch, head.playerId);
        } finally {
            sending = false;
        }

        if (!delivered) {
            return;
        }

        dropExpired();
    }
}

async function sendCommands(batch: QueuedCommand[], playerId: number | null): Promise<boolean> {
    if (batch.length === 0) {
        return true;
    }

    const body = {
        playerId,
        commands: batch.map(item => ({ commandId: item.commandId, kind: item.command.kind, args: item.command.args })),
    };

    const epoch = queueEpoch;
    let answer;
    try {
        answer = await request('POST', 'Domiki/ApplyCommands', commandBatchResultSchema, undefined, body);
    } catch (err) {
        if (!(err instanceof OfflineError || err instanceof MalformedResponseError)) {
            return failBatch(batch, err, epoch);
        }

        try {
            await wait(COMMAND_RETRY_DELAY_MS);
            answer = await request('POST', 'Domiki/ApplyCommands', commandBatchResultSchema, undefined, body);
        } catch (retryErr) {
            return failBatch(batch, retryErr, epoch);
        }
    }

    if (epoch !== queueEpoch) {
        return true;
    }

    inFlight = [];
    clearResend();
    queueChanged();

    const state = gameStateSchema.safeParse(answer.state);
    if (state.success) {
        stateSink?.(state.data);
    }

    const results = new Map(answer.results.map(result => [result.commandId.toLowerCase(), result]));
    for (const item of batch) {
        const result = results.get(item.commandId.toLowerCase());
        if (result == null) {
            settle(item, new ApiError('Деревня не ответила про это действие, попробуйте ещё раз.'));
        } else if (result.status === 'Rejected') {
            settle(item, new ApiError(result.error ?? 'Действие не удалось.'));
        } else {
            settle(item, null);
        }
    }

    return true;
}

function failBatch(batch: QueuedCommand[], err: unknown, epoch: number): boolean {
    if (epoch !== queueEpoch) {
        return true;
    }

    inFlight = [];
    if (err instanceof OfflineError) {
        queue.unshift(...batch);
        notifyCommands();
        scheduleResend();
        return false;
    }

    queueChanged();
    const error = err instanceof Error ? err : new ApiError('Неизвестная ошибка сервера.');
    for (const item of batch) {
        settle(item, error);
    }

    return true;
}

if (typeof window !== 'undefined') {
    window.addEventListener('online', () => {
        clearResend();
        void flushCommands();
    });
}

export async function apiPost(url: string, signal?: AbortSignal, body?: unknown): Promise<void> {
    assertWritable();
    await flushCommands();
    const commandId = newCommandId();
    try {
        await request('POST', url, null, signal, body, commandId);
    } catch (err) {
        if (!(err instanceof OfflineError) || readOnly || signal?.aborted === true) {
            throw err;
        }

        await wait(COMMAND_RETRY_DELAY_MS);
        await request('POST', url, null, signal, body, commandId);
    }
}

export const completeOrder = (orderId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/CompleteOrder/${orderId}`, signal);

export const cancelOrder = (orderId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/CancelOrder/${orderId}`, signal);

export const acceptErrand = (errandId: number, clueId: number, workerIds: number[], signal?: AbortSignal): Promise<void> => {
    const workerIdsQuery = workerIds.map(id => `&workerIds=${id}`).join('');
    return apiPost(`Domiki/AcceptErrand/${errandId}?clueId=${clueId}${workerIdsQuery}`, signal);
};

export const cancelErrand = (errandId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/CancelErrand/${errandId}`, signal);

export const startIncidentSearch = (incidentId: number, clueId: number, workerIds: number[], signal?: AbortSignal): Promise<void> =>
    apiPost('Domiki/StartIncidentSearch', signal, { incidentId, clueId, workerIds });

export const hurryManufacture = (manufactureId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/HurryManufacture/${manufactureId}`, signal);

export const setManufactureAutoRepeat = (manufactureId: number, autoRepeat: boolean, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/SetManufactureAutoRepeat/${manufactureId}?autoRepeat=${String(autoRepeat)}`, signal);

export const setManufactureMeasure = (manufactureId: number, resourceTypeId: number | null, value: number | null, signal?: AbortSignal): Promise<void> =>
    apiPost(resourceTypeId == null || value == null
        ? `Domiki/SetManufactureMeasure/${manufactureId}`
        : `Domiki/SetManufactureMeasure/${manufactureId}?resourceTypeId=${resourceTypeId}&value=${value}`, signal);

export const setResourceReserve = (resourceTypeId: number, reserve: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/SetResourceReserve/${resourceTypeId}?reserve=${reserve}`, signal);

export const hurryDomik = (domikId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/HurryDomik/${domikId}`, signal);

export const getJournalPage = (beforeId: number, group: string, count = 30, signal?: AbortSignal): Promise<JournalPageDto> =>
    apiGet(`Domiki/GetJournalPage?beforeId=${beforeId}&count=${count}&group=${group}`, journalPageSchema, signal);

export const getVillage = (signal?: AbortSignal): Promise<VillageDto> =>
    apiGet('Domiki/GetVillage', villageSchema, signal);

export const setVillage = (name: string, crestIcon: number, crestColor: number, signal?: AbortSignal): Promise<void> =>
    apiPost('Domiki/SetVillage', signal, { name, crestIcon, crestColor });

export const getWorld = (signal?: AbortSignal): Promise<WorldDto> =>
    apiGet('Domiki/GetWorld', worldSchema, signal);

export const visitVillage = (playerId: number, signal?: AbortSignal): Promise<VillageVisitDto> =>
    apiGet(`Domiki/VisitVillage/${playerId}`, villageVisitSchema, signal);

export const leaveGuestbookEntry = (hostPlayerId: number, phraseId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/LeaveGuestbookEntry/${hostPlayerId}?phraseId=${phraseId}`, signal);

export const getGuestbook = (signal?: AbortSignal): Promise<GuestbookDto> =>
    apiGet('Domiki/GetGuestbook', guestbookSchema, signal);

export const helpVillage = async (hostPlayerId: number, signal?: AbortSignal): Promise<HelpResultDto> => {
    assertWritable();
    return request('POST', `Domiki/HelpVillage/${hostPlayerId}`, helpResultSchema, signal);
};

export const startExpedition = (expeditionTypeId: number, workerIds?: number[], provisions?: boolean, signal?: AbortSignal): Promise<void> => {
    const query = [
        ...(workerIds ?? []).map(id => `workerIds=${id}`),
        ...(provisions ? ['provisions=true'] : []),
    ].join('&');
    return apiPost(`Domiki/StartExpedition/${expeditionTypeId}${query ? `?${query}` : ''}`, signal);
};

export const getDecor = (signal?: AbortSignal): Promise<DecorStateDto> =>
    apiGet('Domiki/GetDecor', decorStateSchema, signal);

export const buyDecor = (decorTypeId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/BuyDecor/${decorTypeId}`, signal);

export const setFoodRule = (resourceTypeId: number, reserve: number, forbidden: boolean, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/SetFoodRule/${resourceTypeId}?reserve=${reserve}&forbidden=${String(forbidden)}`, signal);

export const getToloka = (signal?: AbortSignal): Promise<TolokaStateDto | null> =>
    apiGet('Domiki/GetToloka', tolokaStateSchema.nullable(), signal);

export const contributeToloka = (resourceTypeId: number, amount: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/ContributeToloka/${resourceTypeId}/${amount}`, signal);

export const voteToloka = (tolokaTypeId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/VoteToloka/${tolokaTypeId}`, signal);

export const getMarket = (signal?: AbortSignal): Promise<MarketStateDto | null> =>
    apiGet('Domiki/GetMarket', marketStateSchema.nullable(), signal);

export const postLot = (kind: number, giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/PostLot?kind=${kind}&giveResourceTypeId=${giveResourceTypeId}&giveValue=${giveValue}&wantResourceTypeId=${wantResourceTypeId}&wantValue=${wantValue}`, signal);

export const acceptLot = (lotId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/AcceptLot/${lotId}`, signal);

export const cancelLot = (lotId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/CancelLot/${lotId}`, signal);

export const buyFromConvoy = (neighborId: number, resourceTypeId: number, count: number, signal?: AbortSignal): Promise<void> =>
    apiPost('Domiki/BuyFromConvoy', signal, { neighborId, resourceTypeId, count });

export const setFriendNeighbor = (neighborId: number | null, signal?: AbortSignal): Promise<void> =>
    apiPost('Domiki/SetFriendNeighbor', signal, { neighborId });

export const setVillageProfile = (neighborId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/SetVillageProfile?neighborId=${neighborId}`, signal);

export const getMemorialPost = (signal?: AbortSignal): Promise<MemorialPostDto> =>
    apiGet('Domiki/GetMemorialPost', memorialPostSchema, signal);

export const getRelocationPlan = (signal?: AbortSignal): Promise<RelocationPlanDto> =>
    apiGet('Domiki/GetRelocation', relocationPlanSchema, signal);

export const relocate = (valleyId: number, villageName: string | null, signal?: AbortSignal): Promise<void> =>
    apiPost('Domiki/Relocate', signal, { valleyId, villageName });

export const buyPerk = (perkType: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/BuyPerk?perkType=${perkType}`, signal);

export const getPushPublicKey = (signal?: AbortSignal): Promise<string> =>
    apiGet('Push/PublicKey', z.string(), signal);

export const subscribePush = async (subscription: { endpoint: string; p256dh: string; auth: string }, signal?: AbortSignal): Promise<void> => {
    assertWritable();
    await request('POST', 'Push/Subscribe', null, signal, subscription);
};

export const unsubscribePush = async (endpoint: string, signal?: AbortSignal): Promise<void> => {
    assertWritable();
    await request('POST', 'Push/Unsubscribe', null, signal, { endpoint });
};
