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
        json: () => Promise.resolve(body),
    });
}

describe('api envelope', () => {
    beforeEach(() => {
        vi.clearAllMocks();
    });

    it.each<[string, unknown]>([
        ['без ключа content (не-дженерик Response)', { type: 1 }],
        ['с content=null', { type: 1, content: null }],
    ])('apiPost принимает Success-конверт %s', async (_label, body) => {
        mockFetch(body);
        await expect(apiPost('Domiki/BuyDomik/1')).resolves.toBeUndefined();
    });

    it('apiGet возвращает провалидированный content', async () => {
        mockFetch({ type: 1, content: [{ typeId: 1, value: 100 }] });
        const schema = z.array(z.object({ typeId: z.number(), value: z.number() }));
        await expect(apiGet('Domiki/GetResources', schema)).resolves.toEqual([{ typeId: 1, value: 100 }]);
    });

    it('ErrorMessage-конверт бросает ApiError с текстом сервера', async () => {
        mockFetch({ type: 2, content: 'Недостаточно ресурсов.' });
        await expect(apiPost('Domiki/BuyDomik/1')).rejects.toThrow('Недостаточно ресурсов.');
    });

    it('нераспарсиваемое тело бросает ApiError', async () => {
        globalThis.fetch = vi.fn().mockResolvedValue({
            ok: true,
            status: 200,
            json: () => Promise.reject(new SyntaxError('bad json')),
        });
        await expect(apiPost('Domiki/BuyDomik/1')).rejects.toBeInstanceOf(ApiError);
    });
});
