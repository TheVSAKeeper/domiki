import { z } from 'zod';
import { authService } from './auth';
import {
    neighborReputationSchema,
    orderSchema,
    ResponseType,
    type NeighborReputationDto,
    type OrderDto,
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

async function request<T>(method: 'GET' | 'POST', url: string, schema: z.ZodType<T> | null, signal?: AbortSignal): Promise<T> {
    let res: Response;
    try {
        res = await fetch(url, { method, credentials: 'same-origin', signal: signal ?? null });
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
