import { z } from 'zod';
import { authService } from './auth';
import {
    neighborReputationSchema,
    orderSchema,
    ResponseType,
    villageSchema,
    weatherStateSchema,
    workerSchema,
    type NeighborReputationDto,
    type OrderDto,
    type VillageDto,
    type WeatherStateDto,
    type WorkerDto,
} from '../types/api';

export class ApiError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'ApiError';
    }
}

const envelopeSchema = z.object({
    type: z.number(),
    content: z.unknown().optional(),
});

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

    const envelope = envelopeSchema.safeParse(json);
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

export async function apiPost(url: string, signal?: AbortSignal): Promise<void> {
    await request('POST', url, null, signal);
}

export const getOrders = (signal?: AbortSignal): Promise<OrderDto[]> =>
    apiGet('Domiki/GetOrders', orderSchema.array(), signal);

export const getReputation = (signal?: AbortSignal): Promise<NeighborReputationDto[]> =>
    apiGet('Domiki/GetReputation', neighborReputationSchema.array(), signal);

export const completeOrder = (orderId: number, signal?: AbortSignal): Promise<void> =>
    apiPost(`Domiki/CompleteOrder/${orderId}`, signal);

export const getVillage = (signal?: AbortSignal): Promise<VillageDto> =>
    apiGet('Domiki/GetVillage', villageSchema, signal);

export const setVillage = (name: string, crestIcon: number, crestColor: number, signal?: AbortSignal): Promise<void> =>
    request('POST', 'Domiki/SetVillage', null, signal, { name, crestIcon, crestColor });

export const getWorkers = (signal?: AbortSignal): Promise<WorkerDto[]> =>
    apiGet('Domiki/GetWorkers', workerSchema.array(), signal);

export const getWeather = (signal?: AbortSignal): Promise<WeatherStateDto> =>
    apiGet('Domiki/GetWeather', weatherStateSchema, signal);
