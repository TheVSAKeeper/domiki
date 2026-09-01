import { type ReactNode, useEffect, useState } from 'react';
import { authService } from '../../services/auth';
import type { AuthStatus } from '../../services/auth';

interface AuthorizeRouteProps {
    element: ReactNode;
}

export const AuthorizeRoute = ({ element }: AuthorizeRouteProps) => {
    const [status, setStatus] = useState<AuthStatus>('unknown');

    useEffect(() => {
        let active = true;

        const populateAuthenticationState = async () => {
            await authService.isAuthenticated();
            if (active) {
                setStatus(authService.getStatus().status);
            }
        };

        const unsubscribe = authService.subscribe(() => {
            if (active) {
                setStatus(authService.getStatus().status);
            }
        });

        void populateAuthenticationState();

        return () => {
            active = false;
            unsubscribe();
        };
    }, []);

    useEffect(() => {
        if (status === 'anonymous') {
            authService.signIn();
        }
    }, [status]);

    if (status === 'unknown' || status === 'anonymous') {
        return <div></div>;
    }

    return element;
};
