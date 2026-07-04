import { type MouseEvent, useEffect, useState } from 'react';
import { NavItem, NavLink } from 'reactstrap';
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
                <NavItem>
                    <button type="button" className="nav-cta" onClick={loginDemo}>Играть демо</button>
                </NavItem>
                <NavItem>
                    <NavLink tag="a" href="/authentication/login">Войти</NavLink>
                </NavItem>
            </>
        );
    }

    return (
        <>
            <NavItem>
                <span className="nav-user">{userName}</span>
            </NavItem>
            <NavItem>
                <NavLink tag="a" href="/authentication/logout">Выйти</NavLink>
            </NavItem>
        </>
    );
};
