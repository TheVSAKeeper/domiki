import { z } from 'zod';
import { authService } from './auth';
import {
    decorStateSchema,
    expeditionStateSchema,
    gameStateSchema,
    ResponseType,
    responseEnvelopeSchema,
    villageSchema,
    type DecorStateDto,
    type ExpeditionStateDto,
    type GameStateDto,
    type VillageDto,
} from '../types/api';

export class ApiError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'ApiError';
    }
}

const errorMessageType: number = ResponseType.ErrorMessage;

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

    if (envelope.data.type === errorMessageType) {
        const message = typeof envelope.data.content === 'string' ? envelope.data.content : 'Неизвестная ошибка сервера.';
        throw new ApiError(message);
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

export const hurryDomik = (domikId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/HurryDomik/${domikId}`, signal);

export const getVillage = (signal?: AbortSignal): Promise<VillageDto> =>
    apiGet('Domiki/GetVillage', villageSchema, signal);

export const setVillage = (name: string, crestIcon: number, crestColor: number, signal?: AbortSignal): Promise<void> =>
    request('POST', 'Domiki/SetVillage', null, signal, { name, crestIcon, crestColor });

export const getExpeditions = (signal?: AbortSignal): Promise<ExpeditionStateDto> =>
    apiGet('Domiki/GetExpeditions', expeditionStateSchema, signal);

export const startExpedition = (expeditionTypeId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/StartExpedition/${expeditionTypeId}`, signal);

export const getDecor = (signal?: AbortSignal): Promise<DecorStateDto> =>
    apiGet('Domiki/GetDecor', decorStateSchema, signal);

export const buyDecor = (decorTypeId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/BuyDecor/${decorTypeId}`, signal);
