import { z } from 'zod';
import { authService } from './auth';
import {
    decorStateSchema,
    tolokaStateSchema,
    marketStateSchema,
    gameStateSchema,
    problemDetailsSchema,
    responseEnvelopeSchema,
    villageSchema,
    worldSchema,
    villageVisitSchema,
    type DecorStateDto,
    type GameStateDto,
    type TolokaStateDto,
    type MarketStateDto,
    type VillageDto,
    type VillageVisitDto,
    type WorldDto,
} from '../types/api';

export class ApiError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'ApiError';
    }
}

async function request<T>(method: 'GET' | 'POST', url: string, schema: z.ZodType<T> | null, signal?: AbortSignal, body?: unknown): Promise<T> {
    let res: Response;
    try {
        const init: RequestInit = {
            method,
            credentials: 'same-origin',
            signal: signal ?? null,
        };
        if (body != null) {
            init.headers = { 'Content-Type': 'application/json' };
            init.body = JSON.stringify(body);
        }
        res = await fetch(url, init);
    } catch (err) {
        if (signal?.aborted) {
            throw err;
        }
        throw new ApiError('Сеть недоступна. Попробуйте позже.');
    }

    if (res.status === 401) {
        authService.signIn();
        return new Promise<T>(() => {});
    }

    if (!res.ok) {
        const body: unknown = await res.json().catch(() => null);
        const problem = problemDetailsSchema.safeParse(body);
        throw new ApiError(problem.success && problem.data.detail != null ? problem.data.detail : 'Неизвестная ошибка сервера.');
    }

    let json: unknown;
    try {
        json = await res.json();
    } catch {
        throw new ApiError('Некорректный ответ сервера.');
    }

    const envelope = responseEnvelopeSchema.safeParse(json);
    if (!envelope.success) {
        throw new ApiError('Некорректный ответ сервера.');
    }

    if (schema == null) {
        return undefined as T;
    }

    const parsed = schema.safeParse(envelope.data.content);
    if (!parsed.success) {
        throw new ApiError('Сервер вернул данные в неожиданном формате.');
    }

    return parsed.data;
}

export const apiGet = <T>(url: string, schema: z.ZodType<T>, signal?: AbortSignal): Promise<T> =>
    request<T>('GET', url, schema, signal);

export const getGameState = (signal?: AbortSignal): Promise<GameStateDto> =>
    apiGet('Domiki/GetGameState', gameStateSchema, signal);

export async function apiPost(url: string, signal?: AbortSignal): Promise<void> {
    await request('POST', url, null, signal);
}

export const completeOrder = (orderId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/CompleteOrder/${orderId}`, signal);

export const hurryManufacture = (manufactureId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/HurryManufacture/${manufactureId}`, signal);

export const setManufactureAutoRepeat = (manufactureId: number, autoRepeat: boolean, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/SetManufactureAutoRepeat/${manufactureId}?autoRepeat=${String(autoRepeat)}`, signal);

export const hurryDomik = (domikId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/HurryDomik/${domikId}`, signal);

export const getVillage = (signal?: AbortSignal): Promise<VillageDto> =>
    apiGet('Domiki/GetVillage', villageSchema, signal);

export const setVillage = (name: string, crestIcon: number, crestColor: number, signal?: AbortSignal): Promise<void> =>
    request('POST', 'Domiki/SetVillage', null, signal, { name, crestIcon, crestColor });

export const setFeedWorkers = (enabled: boolean, signal?: AbortSignal): Promise<void> =>
    request('POST', 'Domiki/SetFeedWorkers', null, signal, { enabled });

export const getWorld = (signal?: AbortSignal): Promise<WorldDto> =>
    apiGet('Domiki/GetWorld', worldSchema, signal);

export const visitVillage = (playerId: number, signal?: AbortSignal): Promise<VillageVisitDto> =>
    apiGet(`Domiki/VisitVillage/${playerId}`, villageVisitSchema, signal);

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

export const getToloka = (signal?: AbortSignal): Promise<TolokaStateDto | null> =>
    apiGet('Domiki/GetToloka', tolokaStateSchema.nullable(), signal);

export const contributeToloka = (amount: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/ContributeToloka/${amount}`, signal);

export const getMarket = (signal?: AbortSignal): Promise<MarketStateDto | null> =>
    apiGet('Domiki/GetMarket', marketStateSchema.nullable(), signal);

export const postLot = (giveResourceTypeId: number, giveValue: number, wantResourceTypeId: number, wantValue: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/PostLot?giveResourceTypeId=${giveResourceTypeId}&giveValue=${giveValue}&wantResourceTypeId=${wantResourceTypeId}&wantValue=${wantValue}`, signal);

export const acceptLot = (lotId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/AcceptLot/${lotId}`, signal);

export const cancelLot = (lotId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/CancelLot/${lotId}`, signal);

export const getPushPublicKey = (signal?: AbortSignal): Promise<string> =>
    apiGet('Push/PublicKey', z.string(), signal);

export const subscribePush = (subscription: { endpoint: string; p256dh: string; auth: string }, signal?: AbortSignal): Promise<void> =>
    request('POST', 'Push/Subscribe', null, signal, subscription);

export const unsubscribePush = (endpoint: string, signal?: AbortSignal): Promise<void> =>
    request('POST', 'Push/Unsubscribe', null, signal, { endpoint });
