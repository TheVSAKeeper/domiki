import type { ReactNode } from 'react';
import { DomikiPage } from './components/DomikiPage';
import { Home } from './components/Home';
import { Wiki } from './components/Wiki';

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
    {
        path: '/wiki',
        element: <Wiki />,
    },
];

export default AppRoutes;
