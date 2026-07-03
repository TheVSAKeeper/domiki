export class AuthorizeService {
    _callbacks = [];
    _nextSubscriptionId = 0;
    _user = null;

    async isAuthenticated() {
        const user = await this.getUser();
        return !!user;
    }

    async getUser() {
        if (this._user) {
            return this._user;
        }

        const response = await fetch('/authentication/user', { credentials: 'same-origin' });
        if (!response.ok) {
            return null;
        }

        const data = await response.json();
        this._user = data.isAuthenticated ? { name: data.name } : null;
        return this._user;
    }

    signIn(returnUrl) {
        const target = returnUrl || `${window.location.pathname}${window.location.search}`;
        window.location.assign(`/authentication/login?returnUrl=${encodeURIComponent(target)}`);
    }

    signOut() {
        window.location.assign('/authentication/logout');
    }

    subscribe(callback) {
        this._callbacks.push({ callback, subscription: this._nextSubscriptionId++ });
        return this._nextSubscriptionId - 1;
    }

    unsubscribe(subscriptionId) {
        const subscriptionIndex = this._callbacks
            .map((element, index) => element.subscription === subscriptionId ? { found: true, index } : { found: false })
            .filter(element => element.found === true);
        if (subscriptionIndex.length !== 1) {
            throw new Error(`Found an invalid number of subscriptions ${subscriptionIndex.length}`);
        }

        this._callbacks.splice(subscriptionIndex[0].index, 1);
    }

    notifySubscribers() {
        for (let i = 0; i < this._callbacks.length; i++) {
            const callback = this._callbacks[i].callback;
            callback();
        }
    }

    static get instance() { return authService }
}

const authService = new AuthorizeService();

export default authService;
