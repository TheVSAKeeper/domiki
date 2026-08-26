import { clearSnapshot } from './offlineSnapshot';

export type AuthStatus = 'unknown' | 'authenticated' | 'anonymous' | 'error';

interface AuthUser {
    name: string;
}

interface AuthState {
    status: AuthStatus;
    user: AuthUser | null;
}

interface UserResponse {
    isAuthenticated: boolean;
    name: string;
}

class AuthorizeService {
    private _callbacks: (() => void)[] = [];
    private _state: AuthState = { status: 'unknown', user: null };
    private _pending: Promise<AuthState> | null = null;

    constructor() {
        window.addEventListener('online', () => { void this.revalidate(); });
        document.addEventListener('visibilitychange', () => {
            if (document.visibilityState === 'visible') {
                void this.revalidate();
            }
        });
    }

    getStatus(): AuthState {
        return this._state;
    }

    async isAuthenticated(): Promise<boolean> {
        const state = await this.getState();
        return state.status === 'authenticated';
    }

    async getUser(): Promise<AuthUser | null> {
        const state = await this.getState();
        return state.user;
    }

    private async getState(): Promise<AuthState> {
        if (this._state.status !== 'unknown' && this._state.status !== 'error') {
            return this._state;
        }

        if (this._pending) {
            return this._pending;
        }

        this._pending = this.fetchState();
        try {
            this.setState(await this._pending);
        } finally {
            this._pending = null;
        }

        return this._state;
    }

    private async revalidate(): Promise<void> {
        if (this._pending || this._state.status === 'authenticated') {
            return;
        }

        this._pending = this.fetchState();
        try {
            this.setState(await this._pending);
        } finally {
            this._pending = null;
        }
    }

    private setState(next: AuthState): void {
        const changed = next.status !== this._state.status || next.user?.name !== this._state.user?.name;
        this._state = next;

        if (changed) {
            for (const callback of this._callbacks) {
                callback();
            }
        }
    }

    private async fetchState(): Promise<AuthState> {
        let response: Response;
        try {
            response = await fetch('/authentication/user', { credentials: 'same-origin' });
        } catch {
            return { status: 'error', user: null };
        }

        if (!response.ok) {
            return { status: 'error', user: null };
        }

        let payload: unknown;
        try {
            payload = await response.json();
        } catch {
            return { status: 'error', user: null };
        }

        if (payload == null || typeof payload !== 'object' || typeof (payload as UserResponse).isAuthenticated !== 'boolean') {
            return { status: 'error', user: null };
        }

        const data = payload as UserResponse;

        return data.isAuthenticated
            ? { status: 'authenticated', user: { name: data.name } }
            : { status: 'anonymous', user: null };
    }

    signIn(returnUrl?: string): void {
        const target = returnUrl != null && returnUrl.length > 0 ? returnUrl : `${window.location.pathname}${window.location.search}`;
        window.location.assign(`/authentication/login?returnUrl=${encodeURIComponent(target)}`);
    }

    signOut(): void {
        const cleanup = import('./push')
            .then(({ disablePush }) => disablePush())
            .catch(() => undefined);

        void Promise.allSettled([cleanup, clearSnapshot()]).finally(() => {
            window.location.assign('/authentication/logout');
        });
    }

    async loginDemo(): Promise<boolean> {
        try {
            const response = await fetch('/authentication/demo', { method: 'POST', credentials: 'same-origin' });
            return response.ok;
        } catch {
            return false;
        }
    }

    subscribe(callback: () => void): () => void {
        this._callbacks.push(callback);
        return () => {
            const index = this._callbacks.indexOf(callback);
            if (index >= 0) {
                this._callbacks.splice(index, 1);
            }
        };
    }
}

export const authService = new AuthorizeService();
export default authService;
