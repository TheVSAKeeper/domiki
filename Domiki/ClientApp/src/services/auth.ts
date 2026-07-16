interface AuthUser {
    name: string;
}

interface UserResponse {
    isAuthenticated: boolean;
    name: string;
}

class AuthorizeService {
    private _callbacks: (() => void)[] = [];
    private _user: AuthUser | null = null;

    async isAuthenticated(): Promise<boolean> {
        const user = await this.getUser();
        return !!user;
    }

    async getUser(): Promise<AuthUser | null> {
        if (this._user) {
            return this._user;
        }

        const response = await fetch('/authentication/user', { credentials: 'same-origin' });
        if (!response.ok) {
            return null;
        }

        const data = (await response.json()) as UserResponse;
        this._user = data.isAuthenticated ? { name: data.name } : null;
        return this._user;
    }

    signIn(returnUrl?: string): void {
        const target = returnUrl != null && returnUrl.length > 0 ? returnUrl : `${window.location.pathname}${window.location.search}`;
        window.location.assign(`/authentication/login?returnUrl=${encodeURIComponent(target)}`);
    }

    signOut(): void {
        window.location.assign('/authentication/logout');
    }

    async loginDemo(): Promise<boolean> {
        const response = await fetch('/authentication/demo', { method: 'POST', credentials: 'same-origin' });
        return response.ok;
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
