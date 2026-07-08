import '@testing-library/jest-dom/vitest';
import { vi } from 'vitest';

const localStorageMock = {
    getItem: vi.fn(),
    setItem: vi.fn(),
    removeItem: vi.fn(),
    clear: vi.fn(),
};

Object.defineProperty(globalThis, 'localStorage', { value: localStorageMock });

globalThis.fetch = vi.fn().mockResolvedValue({
    ok: true,
    json: () => Promise.resolve({
        authority: 'https://localhost:7157',
        client_id: 'Domiki',
        redirect_uri: 'https://localhost:7157/authentication/login-callback',
        post_logout_redirect_uri: 'https://localhost:7157/authentication/logout-callback',
        response_type: 'id_token token',
        scope: 'DomikiAPI openid profile',
    }),
});

Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: vi.fn().mockImplementation((query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: vi.fn(),
        removeListener: vi.fn(),
        addEventListener: vi.fn(),
        removeEventListener: vi.fn(),
        dispatchEvent: vi.fn(),
    })),
});
