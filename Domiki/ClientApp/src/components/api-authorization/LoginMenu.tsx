import { type MouseEvent, useEffect, useState } from 'react';
import PlayIcon from 'pixelarticons/svg/play.svg?react';
import LoginIcon from 'pixelarticons/svg/login.svg?react';
import LogoutIcon from 'pixelarticons/svg/logout.svg?react';
import UserIcon from 'pixelarticons/svg/user.svg?react';
import { authService } from '../../services/auth';

export const LoginMenu = () => {
    const [isAuthenticated, setIsAuthenticated] = useState(false);
    const [userName, setUserName] = useState<string | null>(null);

    useEffect(() => {
        const populateState = async () => {
            const [authenticated, user] = await Promise.all([authService.isAuthenticated(), authService.getUser()]);
            setIsAuthenticated(authenticated);
            setUserName(user ? user.name : null);
        };

        const subscription = authService.subscribe(populateState);
        void populateState();

        return () => authService.unsubscribe(subscription);
    }, []);

    const loginDemo = async (e: MouseEvent) => {
        e.preventDefault();
        const ok = await authService.loginDemo();
        if (ok) {
            window.location.assign('/domiki-page');
        }
    };

    if (!isAuthenticated) {
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
                <a className="nav-link" href="/authentication/logout">
                    <LogoutIcon className="nav-ico" aria-hidden="true" />
                    Выйти
                </a>
            </li>
        </>
    );
};
