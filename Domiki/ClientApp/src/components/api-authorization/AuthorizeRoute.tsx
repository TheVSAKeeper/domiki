import { type ReactNode, useEffect, useState } from 'react';
import { authService } from '../../services/auth';

interface AuthorizeRouteProps {
    path?: string;
    element: ReactNode;
}

export const AuthorizeRoute = ({ path, element }: AuthorizeRouteProps) => {
    const [ready, setReady] = useState(false);
    const [authenticated, setAuthenticated] = useState(false);

    useEffect(() => {
        let active = true;

        const populateAuthenticationState = async () => {
            const isAuthenticated = await authService.isAuthenticated();
            if (active) {
                setReady(true);
                setAuthenticated(isAuthenticated);
            }
        };

        const subscription = authService.subscribe(() => {
            setReady(false);
            setAuthenticated(false);
            void populateAuthenticationState();
        });

        void populateAuthenticationState();

        return () => {
            active = false;
            authService.unsubscribe(subscription);
        };
    }, []);

    useEffect(() => {
        if (ready && !authenticated) {
            authService.signIn(path);
        }
    }, [ready, authenticated, path]);

    if (!ready || !authenticated) {
        return <div></div>;
    }

    return element;
};
