import { beforeEach, describe, expect, it, vi } from 'vitest';
import { z } from 'zod';
import { apiGet, ApiError, apiPost } from './api';

vi.mock('./auth', () => ({
    authService: { signIn: vi.fn() },
}));

function mockFetch(body: unknown, init: { ok?: boolean; status?: number } = {}) {
    const { ok = true, status = 200 } = init;
    globalThis.fetch = vi.fn().mockResolvedValue({
        ok,
        status,
        headers: new Headers(),
        json: () => Promise.resolve(body),
    });
}

describe('api', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it('apiPost резолвится в undefined на пустое тело 200-ответа', async () => {
        globalThis.fetch = vi.fn().mockResolvedValue({
            ok: true,
            status: 200,
            headers: new Headers(),
            json: () => Promise.reject(new SyntaxError('Unexpected end of JSON input')),
        });
        await expect(apiPost('Domiki/BuyDomik/1')).resolves.toBeUndefined();
    });

    it('apiGet возвращает провалидированное тело ответа', async () => {
        mockFetch([{ typeId: 1, value: 100 }]);
        const schema = z.array(z.object({ typeId: z.number(), value: z.number() }));
        await expect(apiGet('Domiki/GetResources', schema)).resolves.toEqual([{ typeId: 1, value: 100 }]);
    });

    it('ProblemDetails-ответ 400 бросает ApiError с текстом detail', async () => {
        mockFetch(
            {
                type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
                title: 'Bad Request',
                status: 400,
                detail: 'Не хватает монет',
            },
            { ok: false, status: 400 },
        );
        await expect(apiPost('Domiki/BuyDomik/1')).rejects.toThrow('Не хватает монет');
    });

    it('ProblemDetails-ответ 500 без detail бросает ApiError с общим текстом', async () => {
        mockFetch({ type: 'about:blank', title: 'Internal Server Error', status: 500 }, { ok: false, status: 500 });
        await expect(apiPost('Domiki/BuyDomik/1')).rejects.toThrow('Неизвестная ошибка сервера.');
    });

    it('502 с нераспарсиваемым телом бросает ApiError с общим текстом', async () => {
        globalThis.fetch = vi.fn().mockResolvedValue({
            ok: false,
            status: 502,
            headers: new Headers(),
            json: () => Promise.reject(new SyntaxError('bad json')),
        });
        await expect(apiPost('Domiki/BuyDomik/1')).rejects.toThrow('Неизвестная ошибка сервера.');
    });

    it('нераспарсиваемое тело успешного ответа со схемой бросает ApiError', async () => {
        globalThis.fetch = vi.fn().mockResolvedValue({
            ok: true,
            status: 200,
            headers: new Headers(),
            json: () => Promise.reject(new SyntaxError('bad json')),
        });
        const schema = z.array(z.object({ typeId: z.number(), value: z.number() }));
        await expect(apiGet('Domiki/GetResources', schema)).rejects.toBeInstanceOf(ApiError);
    });
});
