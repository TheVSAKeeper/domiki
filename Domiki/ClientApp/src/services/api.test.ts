import { beforeEach, describe, expect, it, vi } from 'vitest';
import { z } from 'zod';
import { apiGet, ApiError, apiPost, OfflineError, setReadOnlyMode } from './api';

vi.mock('./auth', () => ({
    authService: { signIn: vi.fn() },
}));

vi.mock('./offlineSnapshot', () => ({
    clearSnapshot: vi.fn().mockResolvedValue(undefined),
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

    it('сорванный запрос бросает OfflineError, а не общий ApiError', async () => {
        globalThis.fetch = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'));
        await expect(apiGet('Domiki/GetGameState', z.object({}))).rejects.toBeInstanceOf(OfflineError);
    });

    it('ошибка сервера остаётся ApiError и не выдаёт себя за офлайн', async () => {
        mockFetch({ title: 'Server error' }, { ok: false, status: 500 });
        await expect(apiGet('Domiki/GetGameState', z.object({}))).rejects.not.toBeInstanceOf(OfflineError);
    });

    it('в режиме чтения мутация отбивается на клиенте и до сети не доходит', async () => {
        mockFetch(null);
        setReadOnlyMode(true);
        try {
            await expect(apiPost('Domiki/BuyDomik/1')).rejects.toThrow('Без связи деревню можно только смотреть');
            expect(globalThis.fetch).not.toHaveBeenCalled();
        } finally {
            setReadOnlyMode(false);
        }
    });

    it('в режиме чтения чтение состояния продолжает работать', async () => {
        mockFetch([]);
        setReadOnlyMode(true);
        try {
            await expect(apiGet('Domiki/GetResources', z.array(z.unknown()))).resolves.toEqual([]);
        } finally {
            setReadOnlyMode(false);
        }
    });

    it('мутация несёт идентификатор команды', async () => {
        mockFetch(null);
        await apiPost('Domiki/BuyDomik/1');
        const headers = commandHeaders();
        expect(headers).toHaveLength(1);
        expect(headers[0]).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i);
    });

    it('сорванная мутация повторяется один раз с тем же идентификатором команды', async () => {
        globalThis.fetch = vi.fn()
            .mockRejectedValueOnce(new TypeError('Failed to fetch'))
            .mockResolvedValue({ ok: true, status: 200, headers: new Headers(), json: () => Promise.resolve(null) });

        await expect(apiPost('Domiki/BuyDomik/1')).resolves.toBeUndefined();

        const headers = commandHeaders();
        expect(headers).toHaveLength(2);
        expect(headers[0]).toBe(headers[1]);
    });

    it('вторая попытка не делается, если и она сорвалась бы – наружу уходит офлайн-ошибка', async () => {
        globalThis.fetch = vi.fn().mockRejectedValue(new TypeError('Failed to fetch'));
        await expect(apiPost('Domiki/BuyDomik/1')).rejects.toBeInstanceOf(OfflineError);
        expect(globalThis.fetch).toHaveBeenCalledTimes(2);
    });
});

function commandHeaders(): (string | undefined)[] {
    const calls = (globalThis.fetch as unknown as { mock: { calls: [string, RequestInit][] } }).mock.calls;
    return calls.map(([, init]) => (init.headers as Record<string, string>)['X-Command-Id']);
}
