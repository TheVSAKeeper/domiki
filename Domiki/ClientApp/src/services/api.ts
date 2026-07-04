import { authService } from './auth';
import { ApiResult, ResponseType } from '../types/api';

export class ApiError extends Error {
    constructor(message: string) {
        super(message);
        this.name = 'ApiError';
    }
}

async function request<T>(method: 'GET' | 'POST', url: string, signal?: AbortSignal): Promise<T> {
    let res: Response;
    try {
        res = await fetch(url, { method, credentials: 'same-origin', signal });
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

    let data: ApiResult<T>;
    try {
        data = (await res.json()) as ApiResult<T>;
    } catch {
        throw new ApiError('Некорректный ответ сервера.');
    }

    if (data.type === ResponseType.ErrorMessage) {
        throw new ApiError(data.content);
    }

    return (data as { content: T }).content;
}

export const apiGet = <T>(url: string, signal?: AbortSignal): Promise<T> => request<T>('GET', url, signal);

export const apiPost = (url: string, signal?: AbortSignal): Promise<void> => request<void>('POST', url, signal);
