import { ReactNode } from 'react';
import { DomikiPage } from './components/DomikiPage';
import { Home } from './components/Home';

export interface AppRouteConfig {
    index?: boolean;
    path?: string;
    element: ReactNode;
    requireAuth?: boolean;
}

const AppRoutes: AppRouteConfig[] = [
    {
        index: true,
        element: <Home />,
    },
    {
        path: '/domiki-page',
        element: <DomikiPage />,
    },
];

export default AppRoutes;
