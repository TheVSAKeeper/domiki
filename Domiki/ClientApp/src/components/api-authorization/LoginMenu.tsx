import { type MouseEvent, useEffect, useState } from 'react';
import PlayIcon from 'pixelarticons/svg/play.svg?react';
import LoginIcon from 'pixelarticons/svg/login.svg?react';
import LogoutIcon from 'pixelarticons/svg/logout.svg?react';
import UserIcon from 'pixelarticons/svg/user.svg?react';
import { authService } from '../../services/auth';
import type { AuthStatus } from '../../services/auth';

const loginDemo = async (e: MouseEvent) => {
    e.preventDefault();
    const ok = await authService.loginDemo();
    if (ok) {
        window.location.assign('/domiki-page');
    }
};

export const LoginMenu = () => {
    const [status, setStatus] = useState<AuthStatus>('unknown');
    const [userName, setUserName] = useState<string | null>(null);

    useEffect(() => {
        const populateState = async () => {
            await authService.isAuthenticated();
            const state = authService.getStatus();
            setStatus(state.status);
            setUserName(state.user ? state.user.name : null);
        };

        const unsubscribe = authService.subscribe(() => { void populateState(); });
        void populateState();

        return unsubscribe;
    }, []);

    if (status === 'unknown' || status === 'error') {
        return (
            <>
                <li aria-hidden="true"><span className="skeleton-block nav-skeleton-cta"></span></li>
                <li aria-hidden="true"><span className="skeleton-block nav-skeleton-link"></span></li>
            </>
        );
    }

    if (status !== 'authenticated') {
        return (
            <>
                <li>
                    <button type="button" className="nav-cta" onClick={loginDemo}>
                        <PlayIcon className="nav-ico" aria-hidden="true" />
                        Играть демо
                    </button>
                </li>
                <li>
                    <a className="nav-link" href="/authentication/login">
                        <LoginIcon className="nav-ico" aria-hidden="true" />
                        Войти
                    </a>
                </li>
            </>
        );
    }

    return (
        <>
            <li>
                <span className="nav-user">
                    <UserIcon className="nav-ico" aria-hidden="true" />
                    {userName}
                </span>
            </li>
            <li>
                <a className="nav-link" href="/authentication/logout" onClick={event => { event.preventDefault(); authService.signOut(); }}>
                    <LogoutIcon className="nav-ico" aria-hidden="true" />
                    Выйти
                </a>
            </li>
        </>
    );
};
